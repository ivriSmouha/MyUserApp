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
    public class ImageEditorViewModel : BaseViewModel
    {
        private readonly InspectionReportModel _report;
        private readonly IAnnotationService _annotationService;
        private readonly ImageExportService _exportService;
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();
        private readonly Dictionary<string, ObservableCollection<AnnotationModel>> _sessionAnnotations = new Dictionary<string, ObservableCollection<AnnotationModel>>();

        public string ProjectName => _report.ProjectName;
        public ObservableCollection<string> ImageThumbnails { get; }
        public ObservableCollection<AnnotationModel> CurrentAnnotations { get; private set; } = new ObservableCollection<AnnotationModel>();
        public ICollectionView FilteredAnnotations { get; private set; }

        private string _selectedImage;
        public string SelectedImage { get => _selectedImage; set { _selectedImage = value; OnPropertyChanged(); LoadAnnotationsForCurrentImage(); } }

        private AnnotationModel _selectedAnnotation;
        public AnnotationModel SelectedAnnotation
        {
            get => _selectedAnnotation;
            set
            {
                if (_selectedAnnotation != null) _selectedAnnotation.IsSelected = false;
                _selectedAnnotation = value;
                if (_selectedAnnotation != null) _selectedAnnotation.IsSelected = true;
                OnPropertyChanged();
                ((RelayCommand)DeleteAnnotationCommand).RaiseCanExecuteChanged();
            }
        }

        #region Filter Properties
        private bool _showInspectorAnnotations = true;
        public bool ShowInspectorAnnotations { get => _showInspectorAnnotations; set { _showInspectorAnnotations = value; OnPropertyChanged(); FilteredAnnotations?.Refresh(); } }
        private bool _showVerifierAnnotations = true;
        public bool ShowVerifierAnnotations { get => _showVerifierAnnotations; set { _showVerifierAnnotations = value; OnPropertyChanged(); FilteredAnnotations?.Refresh(); } }
        private bool _showAiAnnotations = true;
        public bool ShowAiAnnotations { get => _showAiAnnotations; set { _showAiAnnotations = value; OnPropertyChanged(); FilteredAnnotations?.Refresh(); } }
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

        public ImageEditorViewModel(InspectionReportModel report, UserModel user)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _annotationService = new MockAnnotationService();
            _exportService = new ImageExportService();
            ImageThumbnails = new ObservableCollection<string>(report.ImagePaths);
            UndoCommand = new RelayCommand(Undo, _ => CanUndo());
            RedoCommand = new RelayCommand(Redo, _ => CanRedo());
            DeleteAnnotationCommand = new RelayCommand(DeleteSelectedAnnotation, _ => CanDeleteAnnotation());
            ResetViewCommand = new RelayCommand(ResetView);
            RunAiAnalysisCommand = new RelayCommand(async _ => await RunAiAnalysis(), _ => CanRunAiAnalysis());
            FinishEditingCommand = new RelayCommand(async _ => await FinishEditing());
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));
            SwitchImageCommand = new RelayCommand(imagePath => SelectedImage = imagePath as string);
            SelectedImage = ImageThumbnails.FirstOrDefault();
        }

        public void CreateAndExecuteAddAnnotation(double centerX, double centerY, double radius)
        {
            var newAnnotation = new AnnotationModel { Author = AuthorType.Inspector, CenterX = centerX, CenterY = centerY, Radius = radius };
            var command = new AddAnnotationCommand(CurrentAnnotations, newAnnotation);
            ExecuteCommand(command);
        }

        public void SelectAnnotation(AnnotationModel annotation) => SelectedAnnotation = annotation;
        private bool CanRunAiAnalysis() => !string.IsNullOrEmpty(SelectedImage);
        private async System.Threading.Tasks.Task RunAiAnalysis()
        {
            var aiResults = await _annotationService.GetAnnotationsAsync(SelectedImage);
            if (aiResults.Any())
            {
                foreach (var annotation in aiResults)
                {
                    if (!CurrentAnnotations.Any(a => a.CenterX == annotation.CenterX && a.CenterY == annotation.CenterY))
                    {
                        ExecuteCommand(new AddAnnotationCommand(CurrentAnnotations, annotation));
                    }
                }
            }
            else { MessageBox.Show("AI analysis complete. No issues found."); }
        }

        private async System.Threading.Tasks.Task FinishEditing()
        {
            if (!string.IsNullOrEmpty(SelectedImage) && CurrentAnnotations.Any()) _sessionAnnotations[SelectedImage] = CurrentAnnotations;
            MessageBox.Show("Exporting report images...", "Exporting", MessageBoxButton.OK, MessageBoxImage.Information);
            string outputFolder = await _exportService.ExportImagesAsync(_report, _sessionAnnotations);
            MessageBox.Show($"Export complete! Images saved to:\n{outputFolder}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            OnFinished?.Invoke();
        }

        public void SetTransformOrigin(Point origin) => TransformOrigin = origin;
        public void AdjustZoom(double factor) => ZoomScale = Math.Max(0.5, Math.Min(ZoomScale * factor, 8.0));
        public void AdjustPan(double deltaX, double deltaY) { PanX += deltaX / ZoomScale; PanY += deltaY / ZoomScale; }
        private void Rotate(double angle) => ExecuteCommand(new ChangePropertyValueCommand<double>(this, nameof(RotationAngle), RotationAngle, RotationAngle + angle));

        private bool ApplyAnnotationFilter(object item)
        {
            if (item is AnnotationModel annotation)
            {
                switch (annotation.Author)
                {
                    case AuthorType.Inspector: return ShowInspectorAnnotations;
                    case AuthorType.Verifier: return ShowVerifierAnnotations;
                    case AuthorType.AI: return ShowAiAnnotations;
                    default: return true;
                }
            }
            return false;
        }

        private void ResetView(object obj)
        {
            if (Math.Abs(RotationAngle) > 0.01) ExecuteCommand(new ChangePropertyValueCommand<double>(this, nameof(RotationAngle), RotationAngle, 0));
            Brightness = 1.0; ZoomScale = 1.0; PanX = 0; PanY = 0;
        }

        private void LoadAnnotationsForCurrentImage()
        {
            if (_sessionAnnotations.TryGetValue(SelectedImage, out var previousAnnotations)) CurrentAnnotations = previousAnnotations;
            else CurrentAnnotations = new ObservableCollection<AnnotationModel>();

            FilteredAnnotations = CollectionViewSource.GetDefaultView(CurrentAnnotations);
            FilteredAnnotations.Filter = ApplyAnnotationFilter;
            OnPropertyChanged(nameof(CurrentAnnotations));
            OnPropertyChanged(nameof(FilteredAnnotations));
            _undoStack.Clear(); _redoStack.Clear();
            SelectedAnnotation = null; ResetView(null);
        }

        private bool CanDeleteAnnotation() => SelectedAnnotation != null;
        private void DeleteSelectedAnnotation(object obj)
        {
            if (CanDeleteAnnotation()) ExecuteCommand(new DeleteAnnotationCommand(CurrentAnnotations, SelectedAnnotation));
            SelectedAnnotation = null;
        }

        private bool CanUndo() => _undoStack.Any();
        private bool CanRedo() => _redoStack.Any();
        private void ExecuteCommand(IUndoableCommand command)
        {
            command.Execute(); _undoStack.Push(command); _redoStack.Clear();
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged(); ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }
        private void Undo(object obj)
        {
            var command = _undoStack.Pop(); command.UnExecute(); _redoStack.Push(command); SelectedAnnotation = null;
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged(); ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }
        private void Redo(object obj)
        {
            var command = _redoStack.Pop(); command.Execute(); _undoStack.Push(command); SelectedAnnotation = null;
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged(); ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }
    }
}