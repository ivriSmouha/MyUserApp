using System;
using MyUserApp.ViewModels; // Inheriting from BaseViewModel for INotifyPropertyChanged

namespace MyUserApp.Models
{
    /// <summary>
    /// Represents a single annotation (a colored circle) on an image.
    /// </summary>
    public class AnnotationModel : BaseViewModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public AuthorType Author { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Radius { get; set; }

        private bool _isSelected;
        /// <summary>
        /// Gets or sets a value indicating whether this annotation is currently selected by the user.
        /// This property will trigger a visual change in the UI.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(); // Notify the UI to update its appearance
            }
        }
    }
}