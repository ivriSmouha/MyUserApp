// File: MyUserApp/ViewModels/ImageEditorViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using MyUserApp.ViewModels.Commands;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the SkiaSharp-based Image Editor.
    /// This class manages all the state and logic for image transformations (pan, zoom, rotate),
    /// annotation data, undo/redo commands, and rendering the final image.
    /// </summary>
    public class ImageEditorViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields
        private SKBitmap _currentBitmap;
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();
        private readonly Dictionary<string, ObservableCollection<AnnotationModel>> _sessionAnnotations = new Dictionary<string, ObservableCollection<AnnotationModel>>();

        private enum InteractionMode { None, Drawing, Panning }
        private InteractionMode _currentMode = InteractionMode.None;
        private SKPoint _panStartPoint;
        private SKPoint _drawStartPoint;
        private AnnotationModel _previewAnnotation;
        private readonly InspectionReportModel _report;

        // Transformation state
        private float _zoomScale = 1.0f;
        private SKPoint _panOffset = SKPoint.Empty;
        private float _rotationDegrees = 0f;
        private SKImageInfo _lastCanvasInfo; // Store canvas size for coordinate calculations
        #endregion

        #region Public Properties
        public string ProjectName => _report.ProjectName;
        public ObservableCollection<string> ImageThumbnails { get; }
        public ObservableCollection<AnnotationModel> CurrentAnnotations { get; private set; }

        private string _selectedImage;
        public string SelectedImage
        {
            get => _selectedImage;
            set
            {
                if (_selectedImage == value) return;
                _selectedImage = value;
                OnPropertyChanged();
                LoadImageForEditing();
            }
        }

        private AnnotationModel _selectedAnnotation;
        public AnnotationModel SelectedAnnotation
        {
            get => _selectedAnnotation;
            private set
            {
                if (_selectedAnnotation != null) _selectedAnnotation.IsSelected = false;
                _selectedAnnotation = value;
                if (_selectedAnnotation != null) _selectedAnnotation.IsSelected = true;

                OnPropertyChanged();
                ((RelayCommand)DeleteAnnotationCommand).RaiseCanExecuteChanged();
                InvalidateCanvas();
            }
        }

        #region Filter Properties
        private bool _showInspectorAnnotations = true;
        public bool ShowInspectorAnnotations
        {
            get => _showInspectorAnnotations;
            set { _showInspectorAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); }
        }

        private bool _showVerifierAnnotations = true;
        public bool ShowVerifierAnnotations
        {
            get => _showVerifierAnnotations;
            set { _showVerifierAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); }
        }

        private bool _showAiAnnotations = true;
        public bool ShowAiAnnotations
        {
            get => _showAiAnnotations;
            set { _showAiAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); }
        }
        #endregion
        #endregion

        #region Commands
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand DeleteAnnotationCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand RunAiAnalysisCommand { get; }
        public ICommand FinishEditingCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand RotateRightCommand { get; }
        #endregion

        #region Events
        public event Action RequestCanvasInvalidation;
        public event Action OnFinished;
        #endregion

        #region Constructor and Initialization
        public ImageEditorViewModel(InspectionReportModel report, UserModel user)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            ImageThumbnails = new ObservableCollection<string>(report.ImagePaths);

            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);

            // ===================================================================
            // ==     CORRECTION: Changed lambda from () to _ to match Predicate    ==
            // ===================================================================
            DeleteAnnotationCommand = new RelayCommand(DeleteSelectedAnnotation, _ => SelectedAnnotation != null);

            ResetViewCommand = new RelayCommand(ResetView);
            RunAiAnalysisCommand = new RelayCommand(async _ => await RunAiAnalysis());
            FinishEditingCommand = new RelayCommand(FinishEditing);
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));

            if (ImageThumbnails.Any())
            {
                SelectedImage = ImageThumbnails.First();
            }
        }

        private void LoadImageForEditing()
        {
            _currentBitmap?.Dispose();
            if (string.IsNullOrEmpty(SelectedImage) || !File.Exists(SelectedImage))
            {
                _currentBitmap = null;
                InvalidateCanvas();
                return;
            }

            _currentBitmap = SKBitmap.Decode(SelectedImage);

            if (_sessionAnnotations.TryGetValue(SelectedImage, out var existingAnnotations))
            {
                CurrentAnnotations = existingAnnotations;
            }
            else
            {
                CurrentAnnotations = new ObservableCollection<AnnotationModel>();
                _sessionAnnotations[SelectedImage] = CurrentAnnotations;
            }

            SelectedAnnotation = null;
            ClearHistory();
            ResetView(null);
            OnPropertyChanged(nameof(CurrentAnnotations));
        }
        #endregion

        #region Drawing and Rendering
        public void Draw(SKSurface surface, SKImageInfo info)
        {
            _lastCanvasInfo = info; // Cache canvas info for coordinate transforms
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            if (_currentBitmap == null) return;

            canvas.Save();

            var matrix = GetImageToScreenMatrix(info);
            canvas.SetMatrix(matrix);

            canvas.DrawBitmap(_currentBitmap, 0, 0);

            using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                foreach (var annotation in CurrentAnnotations.Where(ShouldShowAnnotation))
                {
                    paint.Color = GetAnnotationSKColor(annotation);
                    paint.StrokeWidth = annotation.IsSelected ? (4 / _zoomScale) : (2 / _zoomScale);
                    float absCenterX = (float)(annotation.CenterX * _currentBitmap.Width);
                    float absCenterY = (float)(annotation.CenterY * _currentBitmap.Height);
                    float absRadius = (float)(annotation.Radius * _currentBitmap.Width);
                    canvas.DrawOval(absCenterX, absCenterY, absRadius, absRadius, paint);
                }

                if (_previewAnnotation != null)
                {
                    paint.Color = SKColors.White;
                    paint.StrokeWidth = 2 / _zoomScale;
                    paint.PathEffect = SKPathEffect.CreateDash(new float[] { 10 / _zoomScale, 10 / _zoomScale }, 0);
                    float absCenterX = (float)(_previewAnnotation.CenterX * _currentBitmap.Width);
                    float absCenterY = (float)(_previewAnnotation.CenterY * _currentBitmap.Height);
                    float absRadius = (float)(_previewAnnotation.Radius * _currentBitmap.Width);
                    canvas.DrawOval(absCenterX, absCenterY, absRadius, absRadius, paint);
                    paint.PathEffect = null;
                }
            }

            canvas.Restore();
        }

        private void InvalidateCanvas() => RequestCanvasInvalidation?.Invoke();
        #endregion

        #region User Interaction Logic
        public void Zoom(float factor, float anchorX, float anchorY)
        {
            float oldScale = _zoomScale;
            _zoomScale = Math.Max(0.1f, Math.Min(_zoomScale * factor, 10.0f));
            _panOffset.X = anchorX - (_zoomScale / oldScale) * (anchorX - _panOffset.X);
            _panOffset.Y = anchorY - (_zoomScale / oldScale) * (anchorY - _panOffset.Y);
            InvalidateCanvas();
        }

        public void StartPan(float x, float y)
        {
            _currentMode = InteractionMode.Panning;
            _panStartPoint = new SKPoint(x - _panOffset.X, y - _panOffset.Y);
        }

        public void StartDrawing(float x, float y)
        {
            SKPoint? imagePoint = ScreenToImageCoordinates(new SKPoint(x, y));
            if (imagePoint == null) return;

            AnnotationModel clickedAnnotation = CheckForAnnotationHit(imagePoint.Value);
            if (clickedAnnotation != null)
            {
                SelectedAnnotation = clickedAnnotation;
                _currentMode = InteractionMode.None;
                return;
            }

            SelectedAnnotation = null;
            _currentMode = InteractionMode.Drawing;
            _drawStartPoint = imagePoint.Value;
            _previewAnnotation = new AnnotationModel
            {
                Author = AuthorType.Inspector,
                CenterX = _drawStartPoint.X,
                CenterY = _drawStartPoint.Y,
                Radius = 0
            };
        }

        public void UpdateInteraction(float x, float y)
        {
            switch (_currentMode)
            {
                case InteractionMode.Panning:
                    _panOffset = new SKPoint(x - _panStartPoint.X, y - _panStartPoint.Y);
                    InvalidateCanvas();
                    break;
                case InteractionMode.Drawing:
                    SKPoint? currentImagePoint = ScreenToImageCoordinates(new SKPoint(x, y));
                    if (currentImagePoint == null || _previewAnnotation == null) return;
                    double dx = currentImagePoint.Value.X - _drawStartPoint.X;
                    double dy = currentImagePoint.Value.Y - _drawStartPoint.Y;
                    _previewAnnotation.Radius = Math.Sqrt(dx * dx + dy * dy);
                    InvalidateCanvas();
                    break;
            }
        }

        public void EndInteraction(float x, float y)
        {
            if (_currentMode == InteractionMode.Drawing && _previewAnnotation != null)
            {
                if (_currentBitmap != null && _previewAnnotation.Radius * _currentBitmap.Width > 5)
                {
                    ExecuteCommand(new AddAnnotationCommand(CurrentAnnotations, _previewAnnotation));
                    SelectedAnnotation = _previewAnnotation;
                }
            }
            _currentMode = InteractionMode.None;
            _previewAnnotation = null;
            InvalidateCanvas();
        }
        #endregion

        #region Command Methods
        private void ResetView(object _ = null)
        {
            _zoomScale = 1.0f;
            _panOffset = SKPoint.Empty;
            _rotationDegrees = 0f;
            InvalidateCanvas();
        }

        private void Rotate(float angle)
        {
            _rotationDegrees = (_rotationDegrees + angle) % 360;
            InvalidateCanvas();
        }

        private async System.Threading.Tasks.Task RunAiAnalysis()
        {
            var mockService = new MockAnnotationService();
            var aiResults = await mockService.GetAnnotationsAsync(SelectedImage);
            if (aiResults.Any())
            {
                foreach (var annotation in aiResults)
                {
                    ExecuteCommand(new AddAnnotationCommand(CurrentAnnotations, annotation));
                }
            }
            else
            {
                MessageBox.Show("AI analysis complete. No issues found.", "AI Analysis");
            }
        }

        private void DeleteSelectedAnnotation(object _ = null)
        {
            if (SelectedAnnotation == null) return;
            ExecuteCommand(new DeleteAnnotationCommand(CurrentAnnotations, SelectedAnnotation));
            SelectedAnnotation = null;
        }

        private void FinishEditing(object _ = null)
        {
            if (_currentBitmap == null) return;

            string sanitizedProjectName = string.Join("_", _report.ProjectName.Split(Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exported Reports", $"{sanitizedProjectName}_{timestamp}");
            Directory.CreateDirectory(outputDirectory);
            string outputFilePath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(SelectedImage) + "_annotated.jpg");

            using (var exportSurface = SKSurface.Create(new SKImageInfo(_currentBitmap.Width, _currentBitmap.Height)))
            {
                var canvas = exportSurface.Canvas;
                canvas.DrawBitmap(_currentBitmap, 0, 0);

                using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke })
                {
                    foreach (var annotation in CurrentAnnotations)
                    {
                        paint.Color = GetAnnotationSKColor(annotation);
                        paint.StrokeWidth = annotation.IsSelected ? 8 : 4;
                        float absCenterX = (float)(annotation.CenterX * _currentBitmap.Width);
                        float absCenterY = (float)(annotation.CenterY * _currentBitmap.Height);
                        float absRadius = (float)(annotation.Radius * _currentBitmap.Width);
                        canvas.DrawOval(absCenterX, absCenterY, absRadius, absRadius, paint);
                    }
                }

                using (var image = exportSurface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 95))
                using (var stream = File.OpenWrite(outputFilePath))
                {
                    data.SaveTo(stream);
                }
            }

            MessageBox.Show($"Export complete! Image saved to:\n{outputFilePath}", "Success");
            OnFinished?.Invoke();
        }
        #endregion

        #region Undo/Redo Logic
        private void ExecuteCommand(IUndoableCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            InvalidateCanvas();
            UpdateCommandStates();
        }

        private void Undo(object _ = null)
        {
            if (!_undoStack.Any()) return;
            var command = _undoStack.Pop();
            command.UnExecute();
            _redoStack.Push(command);
            InvalidateCanvas();
            UpdateCommandStates();
        }

        private void Redo(object _ = null)
        {
            if (!_redoStack.Any()) return;
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            InvalidateCanvas();
            UpdateCommandStates();
        }

        private bool CanUndo(object _ = null) => _undoStack.Any();
        private bool CanRedo(object _ = null) => _redoStack.Any();
        private void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateCommandStates();
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }
        #endregion

        #region Helper Methods
        private bool ShouldShowAnnotation(AnnotationModel annotation)
        {
            return annotation.Author switch
            {
                AuthorType.Inspector => ShowInspectorAnnotations,
                AuthorType.Verifier => ShowVerifierAnnotations,
                AuthorType.AI => ShowAiAnnotations,
                _ => true
            };
        }

        private SKColor GetAnnotationSKColor(AnnotationModel annotation)
        {
            if (annotation.IsSelected) return SKColors.LimeGreen;
            return annotation.Author switch
            {
                AuthorType.Inspector => SKColors.DodgerBlue,
                AuthorType.Verifier => SKColors.Yellow,
                AuthorType.AI => SKColors.Red,
                _ => SKColors.White
            };
        }

        private SKMatrix GetImageToScreenMatrix(SKImageInfo info)
        {
            if (_currentBitmap == null) return SKMatrix.Identity;

            var matrix = SKMatrix.CreateIdentity();
            float initialOffsetX = (info.Width - _currentBitmap.Width) / 2f;
            float initialOffsetY = (info.Height - _currentBitmap.Height) / 2f;

            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(initialOffsetX + _panOffset.X, initialOffsetY + _panOffset.Y));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateScale(_zoomScale, _zoomScale, info.Width / 2f, info.Height / 2f));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateRotationDegrees(_rotationDegrees, info.Width / 2f, info.Height / 2f));

            return matrix;
        }

        private SKPoint? ScreenToImageCoordinates(SKPoint screenPoint)
        {
            if (_currentBitmap == null) return null;

            var matrix = GetImageToScreenMatrix(_lastCanvasInfo);

            if (!matrix.TryInvert(out var invertedMatrix)) return null;

            SKPoint imagePixelPoint = invertedMatrix.MapPoint(screenPoint);

            if (imagePixelPoint.X < 0 || imagePixelPoint.X > _currentBitmap.Width || imagePixelPoint.Y < 0 || imagePixelPoint.Y > _currentBitmap.Height)
            {
                return null;
            }

            return new SKPoint(imagePixelPoint.X / _currentBitmap.Width, imagePixelPoint.Y / _currentBitmap.Height);
        }

        private AnnotationModel CheckForAnnotationHit(SKPoint relativePoint)
        {
            if (_currentBitmap == null) return null;

            for (int i = CurrentAnnotations.Count - 1; i >= 0; i--)
            {
                var annotation = CurrentAnnotations[i];
                double hitRadius = annotation.Radius + (5 / (_currentBitmap.Width * _zoomScale));
                double dist = Math.Sqrt(Math.Pow(relativePoint.X - annotation.CenterX, 2) + Math.Pow(relativePoint.Y - annotation.CenterY, 2));

                if (dist <= hitRadius)
                {
                    return annotation;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _currentBitmap?.Dispose();
        }
        #endregion
    }
}