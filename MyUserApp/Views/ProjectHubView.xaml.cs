using MyUserApp.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyUserApp.Views
{
    /// <summary>
    /// The code-behind for the ProjectHubView.xaml file.
    /// </summary>
    public partial class ProjectHubView : UserControl
    {
        public ProjectHubView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the MouseDoubleClick event on a DataGrid row, as wired up by the EventSetter in XAML.
        /// This provides a user-friendly way to open a project.
        /// </summary>
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the ViewModel from the view's DataContext.
            if (this.DataContext is ProjectHubViewModel viewModel)
            {
                // Following good MVVM practice, the code-behind does not contain logic itself.
                // It simply invokes the existing OpenProjectCommand on the ViewModel,
                // which already contains all the necessary logic to open the selected project.
                if (viewModel.OpenProjectCommand.CanExecute(null))
                {
                    viewModel.OpenProjectCommand.Execute(null);
                }
            }
        }
    }
}