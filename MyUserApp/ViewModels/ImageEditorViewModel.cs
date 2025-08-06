// File: MyUserApp/ViewModels/ImageEditorViewModel.cs
using MyUserApp.Models;
using MyUserApp.Services;
using MyUserApp.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Image Editor that works with the simplified direct canvas approach.
    /// No complex converters needed - just straightforward data binding and commands.
    /// </summary>
    public class ImageEditorViewModel : BaseViewModel
    {
        private readonly InspectionReportModel _report;
        private readonly IAnnotationService _annotationService;
        private readonly ImageExportService _exportService;
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();
        private readonly Dictionary<string, ObservableCollection<AnnotationModel>> _sessionAnnotations = new Dictionary<string, ObservableCollection<AnnotationModel>>();

        // Basic Properties
        public string ProjectName => _report.ProjectName;
        public ObservableCollection<string> ImageThumbnails { get; }
        public ObservableCollection<AnnotationModel> CurrentAnnotations { get; private set; } = new ObservableCollection<AnnotationModel>();
        public ICollectionView FilteredAnnotations { get; private set; }

        // Selected Image Property
        private string _selectedImage;
        public string SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged();
                LoadAnnotationsForCurrentImage();
            }
        }

        // Selected Annotation Property
        private AnnotationModel _selectedAnnotation;
        public AnnotationModel SelectedAnnotation
        {
            get => _selectedAnnotation;
            set
            {
                // Deselect previous annotation
                if (_selectedAnnotation != null)
                    _selectedAnnotation.IsSelected = false;

                _selectedAnnotation = value;

                // Select new annotation
                if (_selectedAnnotation != null)
                    _selectedAnnotation.IsSelected = true;

                OnPropertyChanged();
                ((RelayCommand)DeleteAnnotationCommand).RaiseCanExecuteChanged();
            }
        }

        #region Filter Properties
        private bool _showInspectorAnnotations = true;
        public bool ShowInspectorAnnotations
        {
            get => _showInspectorAnnotations;
            set
            {
                _showInspectorAnnotations = value;
                OnPropertyChanged();
                FilteredAnnotations?.Refresh();
            }
        }

        private bool _showVerifierAnnotations = true;
        public bool ShowVerifierAnnotations
        {
            get => _showVerifierAnnotations;
            set
            {
                _showVerifierAnnotations = value;
                OnPropertyChanged();
                FilteredAnnotations?.Refresh();
            }
        }

        private bool _showAiAnnotations = true;
        public bool ShowAiAnnotations
        {
            get => _showAiAnnotations;
            set
            {
                _showAiAnnotations = value;
                OnPropertyChanged();
                FilteredAnnotations?.Refresh();
            }
        }
        #endregion

        #region Transformation Properties
        private double _zoomScale = 1.0;
        public double ZoomScale { get => _zoomScale; set { _zoomScale = value; OnPropertyChanged(); } }

        private double _panX = 0;
        public double PanX { get => _panX; set { _panX = value; OnPropertyChanged(); } }

        private double _panY = 0;
        public double PanY { get => _panY; set { _panY = value; OnPropertyChanged(); } }

        private Point _transformOrigin = new Point(0.5, 0.5);
        public Point TransformOrigin { get => _transformOrigin; set { _transformOrigin = value; OnPropertyChanged(); } }

        private double _rotationAngle = 0;
        public double RotationAngle { get => _rotationAngle; set { _rotationAngle = value; OnPropertyChanged(); } }

        private double _brightness = 1.0;
        public double Brightness { get => _brightness; set { _brightness = value; OnPropertyChanged(); } }
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
        public ICommand SwitchImageCommand { get; }
        #endregion

        public event Action OnFinished;

        /// <summary>
        /// Initialize the ViewModel with the inspection report and current user.
        /// </summary>
        public ImageEditorViewModel(InspectionReportModel report, UserModel user)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _annotationService = new MockAnnotationService();
            _exportService = new ImageExportService();

            // Set up image thumbnails
            ImageThumbnails = new ObservableCollection<string>(report.ImagePaths);

            // Initialize commands
            UndoCommand = new RelayCommand(Undo, _ => CanUndo());
            RedoCommand = new RelayCommand(Redo, _ => CanRedo());
            DeleteAnnotationCommand = new RelayCommand(DeleteSelectedAnnotation, _ => CanDeleteAnnotation());
            ResetViewCommand = new RelayCommand(ResetView);
            RunAiAnalysisCommand = new RelayCommand(async _ => await RunAiAnalysis(), _ => CanRunAiAnalysis());
            FinishEditingCommand = new RelayCommand(async _ => await FinishEditing());
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));
            SwitchImageCommand = new RelayCommand(imagePath => SelectedImage = imagePath as string);

            // Select the first image
            SelectedImage = ImageThumbnails.FirstOrDefault();
        }

        /// <summary>
        /// Create and execute a command to add a new annotation.
        /// This method is called from the View when the user finishes drawing a circle.
        /// </summary>
        public void CreateAndExecuteAddAnnotation(double centerX, double centerY, double radius)
        {
            var newAnnotation = new AnnotationModel
            {
                Author = AuthorType.Inspector,
                CenterX = centerX,
                CenterY = centerY,
                Radius = radius
            };
            var command = new AddAnnotationCommand(CurrentAnnotations, newAnnotation);
            ExecuteCommand(command);
        }

        /// <summary>
        /// Select an annotation (or deselect all if null is passed).
        /// </summary>
        public void SelectAnnotation(AnnotationModel annotation)
        {
            SelectedAnnotation = annotation;
        }

        /// <summary>
        /// Check if AI analysis can be run (requires a selected image).
        /// </summary>
        private bool CanRunAiAnalysis() => !string.IsNullOrEmpty(SelectedImage);

        /// <summary>
        /// Run AI analysis on the current image to detect potential issues.
        /// </summary>
        private async System.Threading.Tasks.Task RunAiAnalysis()
        {
            var aiResults = await _annotationService.GetAnnotationsAsync(SelectedImage);
            if (aiResults.Any())
            {
                foreach (var annotation in aiResults)
                {
                    // Only add if not already present
                    if (!CurrentAnnotations.Any(a => System.Math.Abs(a.CenterX - annotation.CenterX) < 0.01 &&
                                                     System.Math.Abs(a.CenterY - annotation.CenterY) < 0.01))
                    {
                        ExecuteCommand(new AddAnnotationCommand(CurrentAnnotations, annotation));
                    }
                }
            }
            else
            {
                MessageBox.Show("AI analysis complete. No issues found.", "AI Analysis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Finish editing and export the annotated images.
        /// </summary>
        private async System.Threading.Tasks.Task FinishEditing()
        {
            // Save current annotations before finishing
            if (!string.IsNullOrEmpty(SelectedImage) && CurrentAnnotations.Any())
            {
                _sessionAnnotations[SelectedImage] = CurrentAnnotations;
            }

            MessageBox.Show("Exporting report images...", "Exporting", MessageBoxButton.OK, MessageBoxImage.Information);
            string outputFolder = await _exportService.ExportImagesAsync(_report, _sessionAnnotations);
            MessageBox.Show($"Export complete! Images saved to:\n{outputFolder}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            OnFinished?.Invoke();
        }

        /// <summary>
        /// Set the transform origin point for zoom operations.
        /// </summary>
        public void SetTransformOrigin(Point origin) => TransformOrigin = origin;

        /// <summary>
        /// Adjust the zoom scale within reasonable limits.
        /// </summary>
        public void AdjustZoom(double factor) => ZoomScale = System.Math.Max(0.1, System.Math.Min(ZoomScale * factor, 10.0));

        /// <summary>
        /// Adjust the pan offset for image navigation.
        /// </summary>
        public void AdjustPan(double deltaX, double deltaY)
        {
            PanX += deltaX / ZoomScale;
            PanY += deltaY / ZoomScale;
        }

        /// <summary>
        /// Rotate the image by the specified angle.
        /// </summary>
        private void Rotate(double angle)
        {
            ExecuteCommand(new ChangePropertyValueCommand<double>(this, nameof(RotationAngle), RotationAngle, RotationAngle + angle));
        }

        /// <summary>
        /// Filter annotations based on current visibility settings.
        /// </summary>
        private bool ApplyAnnotationFilter(object item)
        {
            if (item is AnnotationModel annotation)
            {
                return annotation.Author switch
                {
                    AuthorType.Inspector => ShowInspectorAnnotations,
                    AuthorType.Verifier => ShowVerifierAnnotations,
                    AuthorType.AI => ShowAiAnnotations,
                    _ => true
                };
            }
            return false;
        }

        /// <summary>
        /// Reset all view transformations to default values.
        /// </summary>
        private void ResetView(object obj)
        {
            if (System.Math.Abs(RotationAngle) > 0.01)
            {
                ExecuteCommand(new ChangePropertyValueCommand<double>(this, nameof(RotationAngle), RotationAngle, 0));
            }
            Brightness = 1.0;
            ZoomScale = 1.0;
            PanX = 0;
            PanY = 0;
        }

        /// <summary>
        /// Load annotations for the currently selected image.
        /// </summary>
        private void LoadAnnotationsForCurrentImage()
        {
            // Get annotations for this image if they exist
            if (_sessionAnnotations.TryGetValue(SelectedImage, out var previousAnnotations))
            {
                CurrentAnnotations = previousAnnotations;
            }
            else
            {
                CurrentAnnotations = new ObservableCollection<AnnotationModel>();
            }

            // Set up filtered view
            FilteredAnnotations = CollectionViewSource.GetDefaultView(CurrentAnnotations);
            FilteredAnnotations.Filter = ApplyAnnotationFilter;

            // Notify UI of changes
            OnPropertyChanged(nameof(CurrentAnnotations));
            OnPropertyChanged(nameof(FilteredAnnotations));

            // Clear undo/redo stacks for new image
            _undoStack.Clear();
            _redoStack.Clear();

            // Clear selection
            SelectedAnnotation = null;

            // Reset view
            ResetView(null);
        }

        #region Undo/Redo and Command Execution
        private bool CanDeleteAnnotation() => SelectedAnnotation != null;

        private void DeleteSelectedAnnotation(object obj)
        {
            if (CanDeleteAnnotation())
            {
                ExecuteCommand(new DeleteAnnotationCommand(CurrentAnnotations, SelectedAnnotation));
            }
            SelectedAnnotation = null;
        }

        private bool CanUndo() => _undoStack.Any();
        private bool CanRedo() => _redoStack.Any();

        private void ExecuteCommand(IUndoableCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }

        private void Undo(object obj)
        {
            var command = _undoStack.Pop();
            command.UnExecute();
            _redoStack.Push(command);
            SelectedAnnotation = null;
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }

        private void Redo(object obj)
        {
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            SelectedAnnotation = null;
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }
        #endregion
    }
}