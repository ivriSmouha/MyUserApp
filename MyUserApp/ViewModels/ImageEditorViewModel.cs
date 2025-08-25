using Microsoft.Win32;
using MyUserApp.Models;
using MyUserApp.Services;
using MyUserApp.ViewModels.Commands;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the main image editing screen. It manages the state and logic for
    /// viewing images, drawing annotations, adjusting image properties, and saving reports.
    /// </summary>
    public class ImageEditorViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields
        // Core data and state
        private readonly InspectionReportModel _report;
        private readonly AuthorType _currentUserRole;
        private SKBitmap _currentBitmap;

        // Undo/Redo stacks
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();

        // In-memory collections for the current editing session
        private readonly Dictionary<string, ObservableCollection<AnnotationModel>> _sessionAnnotations = new Dictionary<string, ObservableCollection<AnnotationModel>>();
        private readonly Dictionary<string, ImageAdjustmentModel> _sessionAdjustments = new Dictionary<string, ImageAdjustmentModel>();

        // User interaction state
        private enum InteractionMode { None, Drawing, Panning }
        private InteractionMode _currentMode = InteractionMode.None;
        private SKPoint _interactionStartPoint;
        private SKPoint _panStartOffset;
        private AnnotationModel _previewAnnotation;

        // Canvas view transformation state
        private float _zoomScale = 1.0f;
        private SKPoint _panOffset = SKPoint.Empty;
        private float _rotationDegrees = 0f;
        private SKImageInfo _lastCanvasInfo;

        // Annotation filter state
        private bool _showInspectorAnnotations = true;
        private bool _showVerifierAnnotations = true;
        private bool _showAiAnnotations = true;
        #endregion

        #region Public Properties
        public string ProjectName => _report.ProjectName;
        public ObservableCollection<string> ImageThumbnails { get; }
        public ObservableCollection<AnnotationModel> CurrentAnnotations { get; private set; }

        /// <summary>
        /// Gets a value indicating if there are unsaved changes.
        /// </summary>
        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The currently selected image path for editing.
        /// </summary>
        private string _selectedImage;
        public string SelectedImage { get => _selectedImage; set { if (_selectedImage == value) return; _selectedImage = value; OnPropertyChanged(); ((RelayCommand)DeleteImageCommand).RaiseCanExecuteChanged(); _ = LoadImageForEditingAsync(); } }

        /// <summary>
        /// The currently selected annotation.
        /// </summary>
        private AnnotationModel _selectedAnnotation;
        public AnnotationModel SelectedAnnotation { get => _selectedAnnotation; private set { if (_selectedAnnotation != null) _selectedAnnotation.IsSelected = false; _selectedAnnotation = value; if (_selectedAnnotation != null) _selectedAnnotation.IsSelected = true; OnPropertyChanged(); ((RelayCommand)DeleteAnnotationCommand).RaiseCanExecuteChanged(); InvalidateCanvas(); } }

        #region Filter Properties
        public bool ShowInspectorAnnotations { get => _showInspectorAnnotations; set { _showInspectorAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); } }
        public bool ShowVerifierAnnotations { get => _showVerifierAnnotations; set { _showVerifierAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); } }
        public bool ShowAiAnnotations { get => _showAiAnnotations; set { _showAiAnnotations = value; OnPropertyChanged(); InvalidateCanvas(); } }
        #endregion

        #region Image Adjustment Properties
        public float BrightnessValue
        {
            get
            {
                if (SelectedImage == null || !_sessionAdjustments.ContainsKey(SelectedImage)) return 0f;
                return _sessionAdjustments[SelectedImage].Brightness;
            }
            set
            {
                if (SelectedImage == null) return;
                if (!_sessionAdjustments.ContainsKey(SelectedImage))
                    _sessionAdjustments[SelectedImage] = new ImageAdjustmentModel();
                _sessionAdjustments[SelectedImage].Brightness = value;
                OnPropertyChanged();
                InvalidateCanvas();
                SetDirty();
            }
        }

        public float ContrastValue
        {
            get
            {
                if (SelectedImage == null || !_sessionAdjustments.ContainsKey(SelectedImage)) return 1f;
                return _sessionAdjustments[SelectedImage].Contrast;
            }
            set
            {
                if (SelectedImage == null) return;
                if (!_sessionAdjustments.ContainsKey(SelectedImage))
                    _sessionAdjustments[SelectedImage] = new ImageAdjustmentModel();
                _sessionAdjustments[SelectedImage].Contrast = value;
                OnPropertyChanged();
                InvalidateCanvas();
                SetDirty();
            }
        }
        #endregion

        #region Dropdown Sources
        public ObservableCollection<string> AircraftTypes { get; }
        public ObservableCollection<string> TailNumbers { get; }
        public ObservableCollection<string> AircraftSides { get; }
        public ObservableCollection<string> Reasons { get; }
        public ObservableCollection<string> Usernames { get; }
        #endregion

        #region Report Detail Properties
        public string ReportAircraftType { get => _report.AircraftType; set { if (_report.AircraftType == value) return; _report.AircraftType = value; UpdateProjectNameAndSetDirty(); OnPropertyChanged(); } }
        public string ReportTailNumber { get => _report.TailNumber; set { if (_report.TailNumber == value) return; _report.TailNumber = value; UpdateProjectNameAndSetDirty(); OnPropertyChanged(); } }
        public string ReportAircraftSide { get => _report.AircraftSide; set { if (_report.AircraftSide == value) return; _report.AircraftSide = value; UpdateProjectNameAndSetDirty(); OnPropertyChanged(); } }
        public string ReportReason { get => _report.Reason; set { if (_report.Reason == value) return; _report.Reason = value; SetDirty(); OnPropertyChanged(); } }
        public string ReportInspectorName { get => _report.InspectorName; set { if (_report.InspectorName == value) return; _report.InspectorName = value; SetDirty(); OnPropertyChanged(); } }
        public string ReportVerifierName { get => _report.VerifierName; set { if (_report.VerifierName == value) return; _report.VerifierName = value; SetDirty(); OnPropertyChanged(); } }

        /// <summary>
        /// Updates the project's display name and marks the report as dirty.
        /// </summary>
        private void UpdateProjectNameAndSetDirty()
        {
            _report.ProjectName = $"{_report.AircraftType} - {_report.TailNumber} ({_report.AircraftSide})";
            OnPropertyChanged(nameof(ProjectName));
            SetDirty();
        }
        #endregion

        #region Role Switching Properties
        public bool IsDualRoleUser { get; }
        private AuthorType _activeRole;
        public AuthorType ActiveRole { get => _activeRole; private set { _activeRole = value; OnPropertyChanged(); } }
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
        public ICommand AddImagesCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand SaveOnlyCommand { get; }
        public ICommand ResetBrightnessCommand { get; }
        public ICommand ResetContrastCommand { get; }
        public ICommand SwitchRoleCommand { get; }
        #endregion

        public event Action OnFinished;
        public event Action RequestCanvasInvalidation;

        /// <summary>
        /// Initializes the Image Editor ViewModel with a specific report and user role.
        /// </summary>
        public ImageEditorViewModel(InspectionReportModel report, UserModel user, AuthorType role)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _currentUserRole = role;
            ActiveRole = role;

            // Check if the user is assigned as both Inspector and Verifier.
            IsDualRoleUser = !string.IsNullOrEmpty(report.InspectorName) &&
                             report.InspectorName == user.Username &&
                             report.VerifierName == user.Username;

            ImageThumbnails = new ObservableCollection<string>(report.ImagePaths);

            // Load existing annotations and adjustments into session-specific dictionaries.
            if (report.AnnotationsByImage != null)
            {
                foreach (var entry in report.AnnotationsByImage)
                {
                    _sessionAnnotations[entry.Key] = new ObservableCollection<AnnotationModel>(entry.Value);
                }
            }
            if (report.AdjustmentsByImage != null)
            {
                _sessionAdjustments = new Dictionary<string, ImageAdjustmentModel>(report.AdjustmentsByImage);
            }

            // Populate dropdowns from global services.
            var options = OptionsService.Instance.Options;
            AircraftTypes = new ObservableCollection<string>(options.AircraftTypes);
            TailNumbers = new ObservableCollection<string>(options.TailNumbers);
            AircraftSides = new ObservableCollection<string>(options.AircraftSides);
            Reasons = new ObservableCollection<string>(options.Reasons);
            Usernames = new ObservableCollection<string>(UserService.Instance.Users.Select(u => u.Username));

            // Initialize all ICommand properties.
            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);
            DeleteAnnotationCommand = new RelayCommand(DeleteSelectedAnnotation, _ => SelectedAnnotation != null);
            ResetViewCommand = new RelayCommand(ResetView);
            RunAiAnalysisCommand = new RelayCommand(async _ => await RunAiAnalysis());
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));
            AddImagesCommand = new RelayCommand(AddImages);
            DeleteImageCommand = new RelayCommand(async _ => await DeleteSelectedImageAsync(), _ => !string.IsNullOrEmpty(SelectedImage));
            ResetBrightnessCommand = new RelayCommand(_ => BrightnessValue = 0f, _ => SelectedImage != null);
            ResetContrastCommand = new RelayCommand(_ => ContrastValue = 1f, _ => SelectedImage != null);
            SwitchRoleCommand = new RelayCommand(SwitchActiveRole, _ => IsDualRoleUser);
            FinishEditingCommand = new RelayCommand(async _ => await HandleFinishEditingAsync());
            SaveOnlyCommand = new RelayCommand(async _ => await SaveAndExportFullReportAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAndExportFullReportAsync());
        }

        #region Core Logic
        /// <summary>
        /// Asynchronously initializes the ViewModel, loading the first image.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (ImageThumbnails.Any())
            {
                SelectedImage = ImageThumbnails.First();
            }
            else
            {
                await LoadImageForEditingAsync();
            }
            IsDirty = false;
        }

        /// <summary>
        /// Loads the currently selected image and its associated annotations into the editor.
        /// </summary>
        private async Task LoadImageForEditingAsync()
        {
            _currentBitmap?.Dispose();
            _currentBitmap = null;
            if (string.IsNullOrEmpty(SelectedImage) || !File.Exists(SelectedImage))
            {
                CurrentAnnotations = new ObservableCollection<AnnotationModel>();
                OnPropertyChanged(nameof(CurrentAnnotations));
                InvalidateCanvas();
                return;
            }

            // Load bitmap from file asynchronously.
            using (var tempStream = new MemoryStream(await File.ReadAllBytesAsync(SelectedImage)))
            {
                _currentBitmap = await Task.Run(() => SKBitmap.Decode(tempStream));
            }

            // Set the current annotations list for the selected image.
            if (!_sessionAnnotations.TryGetValue(SelectedImage, out var existingAnnotations))
            {
                existingAnnotations = new ObservableCollection<AnnotationModel>();
                _sessionAnnotations[SelectedImage] = existingAnnotations;
            }
            CurrentAnnotations = existingAnnotations;

            // Reset state for the new image.
            SelectedAnnotation = null;
            ClearHistory();
            ResetView(null);
            OnPropertyChanged(nameof(BrightnessValue));
            OnPropertyChanged(nameof(ContrastValue));
            OnPropertyChanged(nameof(CurrentAnnotations));
        }

        /// <summary>
        /// Saves all changes to the report model and exports the annotated images to a folder.
        /// </summary>
        public async Task SaveAndExportFullReportAsync()
        {
            // Persist session data back to the main report model.
            _report.AnnotationsByImage.Clear();
            foreach (var entry in _sessionAnnotations)
            {
                if (_report.ImagePaths.Contains(entry.Key))
                {
                    _report.AnnotationsByImage[entry.Key] = entry.Value.ToList();
                }
            }
            _report.AdjustmentsByImage = new Dictionary<string, ImageAdjustmentModel>(_sessionAdjustments);
            _report.LastModifiedDate = DateTime.Now;

            ReportService.Instance.UpdateReport(_report);

            if (!_report.ImagePaths.Any())
            {
                MessageBox.Show("Report data saved. No images to export.", "Save Complete");
                IsDirty = false;
                return;
            }

            // Prepare the output directory.
            string sanitizedProjectName = string.Join("_", _report.ProjectName.Split(Path.GetInvalidFileNameChars()));
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exported Reports", sanitizedProjectName);

            try
            {
                DeleteExistingExportFolder();
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not clean the output directory. It may be open or in use.\n\nError: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Process and export each image on a background thread.
            await Task.Run(() =>
            {
                foreach (var imagePath in _report.ImagePaths)
                {
                    if (!File.Exists(imagePath)) continue;

                    _sessionAdjustments.TryGetValue(imagePath, out var adjustments);

                    // Create a new surface to draw the adjusted and annotated image.
                    using (var originalBitmap = SKBitmap.Decode(imagePath))
                    using (var exportSurface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height)))
                    {
                        var canvas = exportSurface.Canvas;

                        // Apply brightness/contrast filter.
                        using (var bitmapPaint = new SKPaint { ColorFilter = CreateColorFilter(adjustments?.Brightness ?? 0f, adjustments?.Contrast ?? 1f) })
                        {
                            canvas.DrawBitmap(originalBitmap, 0, 0, bitmapPaint);
                        }

                        // Draw annotations on top.
                        if (_sessionAnnotations.TryGetValue(imagePath, out var annotationsForThisImage))
                        {
                            using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 })
                            {
                                foreach (var annotation in annotationsForThisImage)
                                {
                                    paint.Color = GetAnnotationSKColor(annotation);
                                    canvas.DrawOval(
                                        (float)(annotation.CenterX * originalBitmap.Width),
                                        (float)(annotation.CenterY * originalBitmap.Height),
                                        (float)(annotation.Radius * originalBitmap.Width),
                                        (float)(annotation.Radius * originalBitmap.Width),
                                        paint
                                    );
                                }
                            }
                        }

                        // Save the final image to the output directory.
                        string outputFilePath = Path.Combine(outputDirectory, Path.GetFileName(imagePath));
                        using (var image = exportSurface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 95))
                        using (var stream = File.OpenWrite(outputFilePath))
                        {
                            data.SaveTo(stream);
                        }
                    }
                }
            });

            MessageBox.Show($"Report saved and all {_report.ImagePaths.Count} images exported to:\n{outputDirectory}", "Export Complete");
            IsDirty = false;
        }

        /// <summary>
        /// Handles the logic for finishing the editing session, prompting to save if needed.
        /// </summary>
        private async Task HandleFinishEditingAsync()
        {
            if (IsDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Would you like to save them before finishing?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    await SaveAndExportFullReportAsync();
                    OnFinished?.Invoke();
                }
                else if (result == MessageBoxResult.No)
                {
                    OnFinished?.Invoke();
                }
            }
            else
            {
                OnFinished?.Invoke();
            }
        }

        /// <summary>
        /// Checks if the view can be closed, prompting to save if there are changes.
        /// </summary>
        public async Task<bool> CanCloseAsync()
        {
            if (!IsDirty) return true;

            var result = MessageBox.Show("You have unsaved changes. Would you like to save them before closing?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await SaveAndExportFullReportAsync();
                return true; // Can close after saving.
            }
            return result == MessageBoxResult.No; // Can close if discarding, cannot close if canceling.
        }
        #endregion

        #region User Interaction and Drawing
        /// <summary>
        /// Main drawing method called by the SkiaSharp canvas view to render the scene.
        /// </summary>
        public void Draw(SKSurface surface, SKImageInfo info)
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);
            if (_currentBitmap == null) return;

            // Refit image if canvas size changes.
            if (_lastCanvasInfo.Width != info.Width || _lastCanvasInfo.Height != info.Height)
            {
                _lastCanvasInfo = info;
                FitImageToView();
            }

            // Apply view transformations (pan, zoom, rotate).
            canvas.Save();
            canvas.Translate(info.Width / 2f, info.Height / 2f);
            canvas.Translate(_panOffset.X, _panOffset.Y);
            canvas.RotateDegrees(_rotationDegrees);
            canvas.Scale(_zoomScale);

            // Draw the main bitmap with adjustments.
            using (var bitmapPaint = new SKPaint { ColorFilter = CreateColorFilter(BrightnessValue, ContrastValue) })
            {
                canvas.DrawBitmap(_currentBitmap, -_currentBitmap.Width / 2f, -_currentBitmap.Height / 2f, bitmapPaint);
            }

            // Draw all visible annotations.
            using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                var annotationsToDraw = (CurrentAnnotations ?? Enumerable.Empty<AnnotationModel>()).Where(ShouldShowAnnotation).ToList();
                if (_previewAnnotation != null)
                {
                    annotationsToDraw.Add(_previewAnnotation);
                }

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

        /// <summary>
        /// Initiates a drawing or selection action based on user input.
        /// </summary>
        public void StartDrawing(float x, float y)
        {
            SKPoint? imagePoint = ScreenToImageCoordinates(new SKPoint(x, y));
            if (imagePoint == null || imagePoint.Value.X < 0 || imagePoint.Value.X > 1 || imagePoint.Value.Y < 0 || imagePoint.Value.Y > 1)
            {
                _currentMode = InteractionMode.None;
                return; // Ignore clicks outside the image bounds.
            }

            AnnotationModel clickedAnnotation = CheckForAnnotationHit(imagePoint.Value);

            if (clickedAnnotation != null)
            {
                // Logic for nesting Verifier/Inspector annotations.
                bool isNestablePair = (clickedAnnotation.Author == AuthorType.Inspector && ActiveRole == AuthorType.Verifier) ||
                                      (clickedAnnotation.Author == AuthorType.Verifier && ActiveRole == AuthorType.Inspector);
                if (isNestablePair)
                {
                    _currentMode = InteractionMode.Drawing;
                    _interactionStartPoint = new SKPoint((float)clickedAnnotation.CenterX, (float)clickedAnnotation.CenterY);
                    _previewAnnotation = new AnnotationModel { Author = ActiveRole, CenterX = clickedAnnotation.CenterX, CenterY = clickedAnnotation.CenterY, Radius = 0 };
                    UpdateInteraction(x, y);
                }
                else if (IsAnnotationEditable(clickedAnnotation))
                {
                    // Select an existing, editable annotation.
                    SelectedAnnotation = clickedAnnotation;
                    _currentMode = InteractionMode.None;
                }
            }
            else
            {
                // Start drawing a new annotation.
                SelectedAnnotation = null;
                _currentMode = InteractionMode.Drawing;
                _interactionStartPoint = imagePoint.Value;
                _previewAnnotation = new AnnotationModel { Author = ActiveRole, CenterX = _interactionStartPoint.X, CenterY = _interactionStartPoint.Y, Radius = 0 };
            }
        }

        /// <summary>
        /// Updates the view based on mouse movement during panning or drawing.
        /// </summary>
        public void UpdateInteraction(float x, float y)
        {
            switch (_currentMode)
            {
                case InteractionMode.Panning:
                    _panOffset = new SKPoint(_panStartOffset.X + (x - _interactionStartPoint.X), _panStartOffset.Y + (y - _interactionStartPoint.Y));
                    InvalidateCanvas();
                    break;
                case InteractionMode.Drawing:
                    SKPoint? currentImagePoint = ScreenToImageCoordinates(new SKPoint(x, y));
                    if (currentImagePoint == null || _previewAnnotation == null) return;

                    // Clamp coordinates to image boundaries to prevent oversized circles.
                    float clampedX = Math.Max(0f, Math.Min(1f, currentImagePoint.Value.X));
                    float clampedY = Math.Max(0f, Math.Min(1f, currentImagePoint.Value.Y));

                    double dx = clampedX - _interactionStartPoint.X;
                    double dy = clampedY - _interactionStartPoint.Y;
                    _previewAnnotation.Radius = Math.Sqrt(dx * dx + dy * dy);
                    InvalidateCanvas();
                    break;
            }
        }

        /// <summary>
        /// Finalizes an interaction, creating an annotation if one was being drawn.
        /// </summary>
        public void EndInteraction(float x, float y)
        {
            if (_currentMode == InteractionMode.Drawing && _previewAnnotation != null)
            {
                // Only add the annotation if it's a meaningful size.
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

        /// <summary>
        /// Zooms the canvas view in or out, centered on the mouse cursor position.
        /// </summary>
        public void Zoom(float factor, float anchorX, float anchorY)
        {
            if (_lastCanvasInfo.Width == 0) return;

            float oldScale = _zoomScale;
            _zoomScale = Math.Clamp(_zoomScale * factor, 0.1f, 10.0f);
            float scaleRatio = _zoomScale / oldScale;

            // Adjust pan offset to keep the anchor point stationary.
            float canvasCenterX = _lastCanvasInfo.Width / 2f;
            float canvasCenterY = _lastCanvasInfo.Height / 2f;
            _panOffset = new SKPoint(
                (anchorX - canvasCenterX) - scaleRatio * (anchorX - canvasCenterX - _panOffset.X),
                (anchorY - canvasCenterY) - scaleRatio * (anchorY - canvasCenterY - _panOffset.Y)
            );
            InvalidateCanvas();
        }

        /// <summary>
        /// Initiates a pan operation.
        /// </summary>
        public void StartPan(float x, float y)
        {
            _currentMode = InteractionMode.Panning;
            _interactionStartPoint = new SKPoint(x, y);
            _panStartOffset = _panOffset;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Sets the IsDirty flag to true.
        /// </summary>
        private void SetDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Creates an SKColorFilter to apply brightness and contrast adjustments.
        /// </summary>
        private SKColorFilter CreateColorFilter(float brightness, float contrast)
        {
            if (brightness == 0f && contrast == 1f) return null;

            float b = brightness;
            var brightnessMatrix = new float[] { 1, 0, 0, 0, b, 0, 1, 0, 0, b, 0, 0, 1, 0, b, 0, 0, 0, 1, 0 };

            float c = contrast;
            float t = (1f - c) / 2f;
            var contrastMatrix = new float[] { c, 0, 0, 0, t, 0, c, 0, 0, t, 0, 0, c, 0, t, 0, 0, 0, 1, 0 };

            return SKColorFilter.CreateCompose(
                SKColorFilter.CreateColorMatrix(brightnessMatrix),
                SKColorFilter.CreateColorMatrix(contrastMatrix)
            );
        }

        /// <summary>
        /// Opens a file dialog to add new images to the report.
        /// </summary>
        private void AddImages(object obj)
        {
            DeleteExistingExportFolder();
            SetDirty();
            var openFileDialog = new OpenFileDialog { Multiselect = true, Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp", Title = "Select Images to Add" };
            if (openFileDialog.ShowDialog() == true)
            {
                string reportImageFolder = GetOrCreateReportImageFolder();
                var newlyAddedPaths = new List<string>();
                foreach (string originalPath in openFileDialog.FileNames)
                {
                    string destinationPath = Path.Combine(reportImageFolder, Path.GetFileName(originalPath));
                    try
                    {
                        File.Copy(originalPath, destinationPath, true);
                        if (!_report.ImagePaths.Contains(destinationPath))
                        {
                            _report.ImagePaths.Add(destinationPath);
                            ImageThumbnails.Add(destinationPath);
                            newlyAddedPaths.Add(destinationPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error copying file {Path.GetFileName(originalPath)}: {ex.Message}", "File Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                if (newlyAddedPaths.Any())
                {
                    SaveAndExportFullReportAsync();
                    SelectedImage = newlyAddedPaths.First();
                }
            }
        }

        /// <summary>
        /// Deletes the currently selected image and its data from the report.
        /// </summary>
        private async Task DeleteSelectedImageAsync()
        {
            string imageToDelete = SelectedImage;
            if (string.IsNullOrEmpty(imageToDelete)) return;
            var result = MessageBox.Show("Are you sure you want to permanently delete this image and all its annotations?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            DeleteExistingExportFolder();
            SetDirty();

            // Determine which image to select next.
            int deletedImageIndex = ImageThumbnails.IndexOf(imageToDelete);
            string nextSelection = (ImageThumbnails.Count > 1) ? (deletedImageIndex == 0 ? ImageThumbnails[1] : ImageThumbnails[deletedImageIndex - 1]) : null;

            // Clear selection and remove data.
            SelectedImage = null;
            await Task.Delay(100); // Allow UI to update.
            _report.ImagePaths.Remove(imageToDelete);
            _sessionAnnotations.Remove(imageToDelete);
            _sessionAdjustments.Remove(imageToDelete);
            ImageThumbnails.Remove(imageToDelete);

            // Delete the physical file.
            try
            {
                if (File.Exists(imageToDelete)) File.Delete(imageToDelete);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete the image file:\n{ex.Message}", "File Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Resave the report and select the next image.
            await SaveAndExportFullReportAsync();
            SelectedImage = nextSelection;
        }

        /// <summary>
        /// Executes an undoable command and manages the undo/redo stacks.
        /// </summary>
        private void ExecuteCommand(IUndoableCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            SetDirty();
            InvalidateCanvas();
            UpdateCommandStates();
        }

        /// <summary>
        /// Deletes the old export folder to ensure a clean export.
        /// </summary>
        private void DeleteExistingExportFolder()
        {
            try
            {
                string sanitizedProjectName = string.Join("_", _report.ProjectName.Split(Path.GetInvalidFileNameChars()));
                string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exported Reports", sanitizedProjectName);
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not delete old export folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets or creates the dedicated folder for storing this report's images.
        /// </summary>
        private string GetOrCreateReportImageFolder()
        {
            string folderPath;
            if (_report.ImagePaths.Any())
            {
                folderPath = Path.GetDirectoryName(_report.ImagePaths.First());
            }
            else
            {
                string reportImagesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportImages");
                folderPath = Path.Combine(reportImagesRoot, _report.ReportId.ToString());
            }
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }

        /// <summary>
        /// Resets the view to fit the image within the canvas.
        /// </summary>
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

        /// <summary>
        /// Rotates the image by a specified angle.
        /// </summary>
        private void Rotate(float angle)
        {
            _rotationDegrees = (_rotationDegrees + angle) % 360;
            InvalidateCanvas();
            SetDirty();
        }

        /// <summary>
        /// Runs a simulated AI analysis to generate annotations.
        /// </summary>
        private async Task RunAiAnalysis()
        {
            if (string.IsNullOrEmpty(SelectedImage))
            {
                MessageBox.Show("Please select an image before running AI analysis.", "No Image Selected");
                return;
            }
            SetDirty();
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
            if (SelectedAnnotation == null || !IsAnnotationEditable(SelectedAnnotation)) return;
            ExecuteCommand(new DeleteAnnotationCommand(CurrentAnnotations, SelectedAnnotation));
            SelectedAnnotation = null;
        }

        private void Undo(object _ = null)
        {
            if (!CanUndo()) return;
            var command = _undoStack.Pop();
            command.UnExecute();
            _redoStack.Push(command);
            InvalidateCanvas();
            UpdateCommandStates();
            IsDirty = _undoStack.Any();
        }

        private void Redo(object _ = null)
        {
            if (!CanRedo()) return;
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            InvalidateCanvas();
            UpdateCommandStates();
            SetDirty();
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

        /// <summary>
        /// Converts screen coordinates (from the UI) to relative image coordinates (0-1).
        /// </summary>
        private SKPoint? ScreenToImageCoordinates(SKPoint screenPoint)
        {
            if (_currentBitmap == null || _lastCanvasInfo.Width == 0) return null;

            // Build the transformation matrix from image to screen.
            var matrix = SKMatrix.CreateIdentity();
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(-_currentBitmap.Width / 2f, -_currentBitmap.Height / 2f));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateScale(_zoomScale, _zoomScale));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateRotationDegrees(_rotationDegrees));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(_panOffset.X, _panOffset.Y));
            SKMatrix.PostConcat(ref matrix, SKMatrix.CreateTranslation(_lastCanvasInfo.Width / 2f, _lastCanvasInfo.Height / 2f));

            // Invert it to go from screen to image.
            if (!matrix.TryInvert(out var invertedMatrix)) return null;

            SKPoint imagePixelPoint = invertedMatrix.MapPoint(screenPoint);
            return new SKPoint(imagePixelPoint.X / _currentBitmap.Width, imagePixelPoint.Y / _currentBitmap.Height);
        }

        /// <summary>
        /// Checks if a click at a given point hits any existing annotation.
        /// </summary>
        private AnnotationModel CheckForAnnotationHit(SKPoint relativePoint)
        {
            if (_currentBitmap == null || CurrentAnnotations == null) return null;
            // Iterate backwards to prioritize annotations on top.
            for (int i = CurrentAnnotations.Count - 1; i >= 0; i--)
            {
                var annotation = CurrentAnnotations[i];
                double dist = Math.Sqrt(Math.Pow(relativePoint.X - annotation.CenterX, 2) + Math.Pow(relativePoint.Y - annotation.CenterY, 2));
                // Add a small buffer to make selection easier.
                double relativeHitRadius = annotation.Radius + (5 / (_currentBitmap.Width * _zoomScale));
                if (dist <= relativeHitRadius) return annotation;
            }
            return null;
        }

        /// <summary>
        /// Toggles the active role between Inspector and Verifier for dual-role users.
        /// </summary>
        private void SwitchActiveRole(object obj)
        {
            ActiveRole = (ActiveRole == AuthorType.Inspector) ? AuthorType.Verifier : AuthorType.Inspector;
        }

        /// <summary>
        /// Determines if the current user is allowed to edit a given annotation.
        /// </summary>
        private bool IsAnnotationEditable(AnnotationModel annotation)
        {
            if (annotation == null) return false;
            // Dual-role users can edit both their inspector and verifier marks.
            if (IsDualRoleUser)
            {
                return annotation.Author == AuthorType.Inspector || annotation.Author == AuthorType.Verifier;
            }
            // Standard users can only edit annotations matching their role.
            return annotation.Author == _currentUserRole;
        }

        /// <summary>
        /// Releases the SKBitmap resource.
        /// </summary>
        public void Dispose()
        {
            _currentBitmap?.Dispose();
        }
        #endregion
    }
}