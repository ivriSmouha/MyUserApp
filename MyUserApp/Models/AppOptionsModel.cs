using System.Collections.ObjectModel;

namespace MyUserApp.Models
{
    /// <summary>
    /// A data model that holds collections of selectable options
    /// used to populate dropdown menus throughout the application.
    /// </summary>
    public class AppOptionsModel
    {
        /// <summary>
        /// A list of available aircraft types.
        /// </summary>
        public ObservableCollection<string> AircraftTypes { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// A list of available aircraft tail numbers.
        /// </summary>
        public ObservableCollection<string> TailNumbers { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// A list of available aircraft sides or sections.
        /// </summary>
        public ObservableCollection<string> AircraftSides { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// A list of predefined reasons for an inspection.
        /// </summary>
        public ObservableCollection<string> Reasons { get; set; } = new ObservableCollection<string>();
    }
}