using System.Windows.Controls;

namespace MyUserApp.Views
{
    /// <summary>
    /// The code-behind for the ReportEntryView.xaml file.
    /// This view is "pure MVVM" and contains no business logic. All interactions and data
    /// are handled by its corresponding ViewModel, ReportEntryViewModel.
    /// </summary>
    public partial class ReportEntryView : UserControl
    {
        public ReportEntryView()
        {
            InitializeComponent();
        }
    }
}