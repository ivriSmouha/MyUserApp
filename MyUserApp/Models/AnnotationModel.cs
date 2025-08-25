using System;
using MyUserApp.ViewModels; // Inherited for property change notifications.

namespace MyUserApp.Models
{
    /// <summary>
    /// Defines the data structure for a single annotation placed on an image.
    /// This class also includes UI-related properties for selection state.
    /// </summary>
    public class AnnotationModel : BaseViewModel
    {
        /// <summary>
        /// A unique identifier for the annotation.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The type of author who created the annotation (e.g., Inspector, Verifier, AI).
        /// </summary>
        public AuthorType Author { get; set; }

        /// <summary>
        /// The relative X-coordinate of the annotation's center (from 0.0 to 1.0).
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// The relative Y-coordinate of the annotation's center (from 0.0 to 1.0).
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// The relative radius of the annotation, typically based on the image width.
        /// </summary>
        public double Radius { get; set; }

        // Private backing field for the IsSelected property.
        private bool _isSelected;

        /// <summary>
        /// Gets or sets a value indicating if the annotation is currently selected in the UI.
        /// When set, it notifies the UI to update its appearance.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(); // Triggers the PropertyChanged event for UI updates.
            }
        }
    }
}