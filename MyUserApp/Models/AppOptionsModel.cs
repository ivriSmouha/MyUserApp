using System.Collections.Generic;
namespace MyUserApp.Models
{
    // This class holds the lists of choices for all dropdowns in the app.
    public class AppOptionsModel
    {
        public List<string> AircraftTypes { get; set; } = new List<string>();
        public List<string> AircraftSides { get; set; } = new List<string>();
        public List<string> Reasons { get; set; } = new List<string>();
    }
}