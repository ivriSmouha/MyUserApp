// Models/AppOptionsModel.cs
using System.Collections.ObjectModel;

namespace MyUserApp.Models
{
    // This class holds the lists of choices for all dropdowns in the app.
    public class AppOptionsModel
    {
        public ObservableCollection<string> AircraftTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AircraftSides { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Reasons { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> TailNumbers { get; set; } = new ObservableCollection<string>();
    }
}