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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class ImageEditorViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields
        // ... (other private fields are unchanged)
        private readonly InspectionReportModel _report;
        private readonly AuthorType _currentUserRole; // NEW: Store the user's role for this session.

        private SKBitmap _currentBitmap;
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();
        private readonly Dictionary<string, ObservableCollection<AnnotationModel>> _sessionAnnotations = new Dictionary<string, ObservableCollection<AnnotationModel>>();

        private enum InteractionMode { None, Drawing, Panning }
        private InteractionMode _currentMode = InteractionMode.None;
        private SKPoint _panStartPoint;
        private SKPoint _drawStartPoint;
        private AnnotationModel _previewAnnotation;

        private float _zoomScale = 1.0f;
        private SKPoint _panOffset = SKPoint.Empty;
        private float _rotationDegrees = 0f;
        private SKImageInfo _lastCanvasInfo;
        #endregion

        // Properties and other sections are mostly unchanged...
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
                _ = LoadImageForEditingAsync();
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
        public bool ShowInspectorAnnotations { get => _showInspectorAnnotations; set { _showInspectorAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); } }
        private bool _showVerifierAnnotations = true;
        public bool ShowVerifierAnnotations { get => _showVerifierAnnotations; set { _showVerifierAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); } }
        private bool _showAiAnnotations = true;
        public bool ShowAiAnnotations { get => _showAiAnnotations; set { _showAiAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); } }
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
        public ICommand SaveCommand { get; }
        #endregion

        public event Action RequestCanvasInvalidation;
        public event Action OnFinished;

        // ===================================================================
        // ==    CHANGE: The constructor now accepts the user's role        ==
        // ===================================================================
        public ImageEditorViewModel(InspectionReportModel report, UserModel user, AuthorType role)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _currentUserRole = role; // Store the role
            ImageThumbnails = new ObservableCollection<string>(report.ImagePaths);

            if (report.AnnotationsByImage != null)
            {
                foreach (var entry in report.AnnotationsByImage)
                {
                    _sessionAnnotations[entry.Key] = new ObservableCollection<AnnotationModel>(entry.Value);
                }
            }

            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);
            DeleteAnnotationCommand = new RelayCommand(DeleteSelectedAnnotation, _ => SelectedAnnotation != null);
            ResetViewCommand = new RelayCommand(ResetView);
            RunAiAnalysisCommand = new RelayCommand(async _ => await RunAiAnalysis());
            FinishEditingCommand = new RelayCommand(FinishEditing);
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));
            SaveCommand = new RelayCommand(_ => SaveAnnotations());
        }

        public async Task InitializeAsync()
        {
            if (ImageThumbnails.Any())
            {
                _selectedImage = ImageThumbnails.First();
                OnPropertyChanged(nameof(SelectedImage));
                await LoadImageForEditingAsync();
            }
        }

        private async Task LoadImageForEditingAsync()
        {
            _currentBitmap?.Dispose();
            if (string.IsNullOrEmpty(SelectedImage) || !File.Exists(SelectedImage))
            {
                _currentBitmap = null;
                InvalidateCanvas();
                return;
            }

            var newBitmap = await Task.Run(() => SKBitmap.Decode(SelectedImage));
            _currentBitmap?.Dispose();
            _currentBitmap = newBitmap;

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

        public void Draw(SKSurface surface, SKImageInfo info)
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            if (_currentBitmap == null) return;

            if (_lastCanvasInfo.Width != info.Width || _lastCanvasInfo.Height != info.Height)
            {
                _lastCanvasInfo = info;
                FitImageToView();
            }

            canvas.Save();

            canvas.Translate(info.Width / 2f, info.Height / 2f);
            canvas.Translate(_panOffset.X, _panOffset.Y);
            canvas.RotateDegrees(_rotationDegrees);
            canvas.Scale(_zoomScale);
            canvas.DrawBitmap(_currentBitmap, -_currentBitmap.Width / 2f, -_currentBitmap.Height / 2f);

            using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                var annotationsToDraw = CurrentAnnotations.Where(ShouldShowAnnotation).ToList();
                if (_previewAnnotation != null) annotationsToDraw.Add(_previewAnnotation);

                foreach (var annotation in annotationsToDraw)
                {
                    bool isPreview = annotation == _previewAnnotation;
                    paint.Color = isPreview ? SKColors.White : GetAnnotationSKColor(annotation);
                    paint.StrokeWidth = isPreview || annotation.IsSelected ? (4 / _zoomScale) : (2 / _zoomScale);
                    paint.PathEffect = isPreview ? SKPathEffect.CreateDash(new float[] { 10 / _zoomScale, 10 / _zoomScale }, 0) : null;

                    float circleX = (float)((annotation.CenterX - 0.5) * _currentBitmap.Width);
                    float circleY = (float)((annotation.CenterY - 0.5) * _currentBitmap.Height);
                    float circleR = (float)(annotation.Radius * _currentBitmap.Width);

                    canvas.DrawOval(circleX, circleY, circleR, circleR, paint);
                }
            }

            canvas.Restore();
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

            // ===================================================================
            // ==     CHANGE: New annotations now use the user's session role   ==
            // ===================================================================
            _previewAnnotation = new AnnotationModel
            {
                Author = _currentUserRole, // Use the role passed into the constructor
                CenterX = _drawStartPoint.X,
                CenterY = _drawStartPoint.Y,
                Radius = 0
            };
        }

        // --- Omitted the rest of the file for brevity, as it is unchanged ---
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

        private void FitImageToView()
        {
            if (_currentBitmap == null || _lastCanvasInfo.Width == 0) return;
            var widthScale = (float)_lastCanvasInfo.Width / _currentBitmap.Width;
            var heightScale = (float)_lastCanvasInfo.Height / _currentBitmap.Height;
            _zoomScale = Math.Min(widthScale, heightScale);
            _panOffset = SKPoint.Empty;
            _rotationDegrees = 0f;
            InvalidateCanvas();
        }

        private void ResetView(object _ = null)
        {
            FitImageToView();
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

        public void DeleteSelectedAnnotation(object _ = null)
        {
            if (SelectedAnnotation == null) return;
            ExecuteCommand(new DeleteAnnotationCommand(CurrentAnnotations, SelectedAnnotation));
            SelectedAnnotation = null;
        }

        public void SaveAnnotations()
        {
            _report.AnnotationsByImage.Clear();
            foreach (var entry in _sessionAnnotations)
            {
                _report.AnnotationsByImage[entry.Key] = entry.Value.ToList();
            }
            ReportService.Instance.UpdateReport(_report);
        }

        private void FinishEditing(object _ = null)
        {
            if (_currentBitmap == null) return;

            SaveAnnotations();

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

            MessageBox.Show($"Report saved and exported to:\n{outputDirectory}", "Success");
            OnFinished?.Invoke();
        }

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

        private void InvalidateCanvas() => RequestCanvasInvalidation?.Invoke();
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
        private SKMatrix GetScreenToImageMatrix()
        {
            if (_currentBitmap == null || _lastCanvasInfo.Width == 0) return SKMatrix.Identity;
            var matrix = SKMatrix.CreateIdentity();
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(-_currentBitmap.Width / 2f, -_currentBitmap.Height / 2f));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateScale(_zoomScale, _zoomScale));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateRotationDegrees(_rotationDegrees));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(_panOffset.X, _panOffset.Y));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(_lastCanvasInfo.Width / 2f, _lastCanvasInfo.Height / 2f));
            if (!matrix.TryInvert(out var invertedMatrix)) return SKMatrix.Identity;
            return invertedMatrix;
        }
        private SKPoint? ScreenToImageCoordinates(SKPoint screenPoint)
        {
            if (_currentBitmap == null) return null;
            var invertedMatrix = GetScreenToImageMatrix();
            SKPoint imagePixelPoint = invertedMatrix.MapPoint(screenPoint);
            return new SKPoint(imagePixelPoint.X / _currentBitmap.Width, imagePixelPoint.Y / _currentBitmap.Height);
        }
        private AnnotationModel CheckForAnnotationHit(SKPoint relativePoint)
        {
            if (_currentBitmap == null) return null;
            for (int i = CurrentAnnotations.Count - 1; i >= 0; i--)
            {
                var annotation = CurrentAnnotations[i];
                double dist = Math.Sqrt(Math.Pow(relativePoint.X - annotation.CenterX, 2) + Math.Pow(relativePoint.Y - annotation.CenterY, 2));
                double relativeHitRadius = annotation.Radius + (5 / (_currentBitmap.Width * _zoomScale));
                if (dist <= relativeHitRadius) return annotation;
            }
            return null;
        }
        public void Dispose()
        {
            _currentBitmap?.Dispose();
        }
    }
}