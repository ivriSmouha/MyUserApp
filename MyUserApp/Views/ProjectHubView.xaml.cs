using MyUserApp.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyUserApp.Views
{
    /// <summary>
    /// Interaction logic for ProjectHubView.xaml
    /// </summary>
    public partial class ProjectHubView : UserControl
    {
        public ProjectHubView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This event handler is called when a ListViewItem is double-clicked,
        /// as defined by the EventSetter in the XAML.
        /// </summary>
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the ViewModel from the view's DataContext.
            if (this.DataContext is ProjectHubViewModel viewModel)
            {
                // Check if the command can be executed and then run it.
                // The command will use the currently selected project from the ViewModel.
                if (viewModel.OpenProjectCommand.CanExecute(null))
                {
                    viewModel.OpenProjectCommand.Execute(null);
                }
            }
        }
    }
}