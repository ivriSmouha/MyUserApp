using System.Windows.Controls;

namespace MyUserApp.Views
{
    /// <summary>
    /// The code-behind for the AdminPanelView.xaml file.
    /// This view is "pure MVVM" and contains no business logic. All interactions and data
    /// are handled by its corresponding ViewModel, AdminPanelViewModel.
    /// </summary>
    public partial class AdminPanelView : UserControl
    {
        public AdminPanelView()
        {
            InitializeComponent();
        }
    }
}