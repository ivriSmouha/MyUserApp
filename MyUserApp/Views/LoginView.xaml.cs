using MyUserApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MyUserApp.Views
{
    /// <summary>
    /// The code-behind for the LoginView.xaml file.
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the PasswordChanged event for the PasswordBox.
        /// This is a necessary workaround because, for security reasons, the PasswordBox's 'Password' property
        /// is not a dependency property and cannot be directly data-bound in XAML.
        /// This method manually updates the Password property on the ViewModel whenever the user types.
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Check if the DataContext is a LoginViewModel to avoid errors.
            if (this.DataContext is LoginViewModel viewModel)
            {
                // Update the ViewModel's Password property with the content of the PasswordBox.
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}