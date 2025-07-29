// Models/AppOptionsModel.cs
using System.Collections.ObjectModel;

namespace MyUserApp.Models
{
    public class AppOptionsModel
    {
        public ObservableCollection<string> AircraftTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> TailNumbers { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AircraftSides { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Reasons { get; set; } = new ObservableCollection<string>();
    }
}