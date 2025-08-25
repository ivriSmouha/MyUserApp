using MyUserApp.Models;
using System;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Login screen. It handles user input and authentication logic.
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        /// <summary>
        /// The username entered by the user.
        /// </summary>
        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The password entered by the user.
        /// </summary>
        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to execute when the user clicks the login button.
        /// </summary>
        public ICommand LoginCommand { get; }

        /// <summary>
        /// An action that is invoked upon successful login, passing the authenticated user model.
        /// </summary>
        public Action<UserModel> OnLoginSuccess { get; set; }

        /// <summary>
        /// Initializes a new instance of the LoginViewModel.
        /// </summary>
        /// <param name="authenticateUser">A function delegate that handles the actual user authentication.</param>
        public LoginViewModel(Func<string, string, UserModel> authenticateUser)
        {
            LoginCommand = new RelayCommand(param =>
            {
                // Attempt to authenticate the user with the provided credentials.
                UserModel user = authenticateUser(Username, Password);
                if (user != null)
                {
                    // If authentication is successful, invoke the success action.
                    OnLoginSuccess?.Invoke(user);
                }
                else
                {
                    // If authentication fails, show an error message.
                    System.Windows.MessageBox.Show("Invalid username or password.");
                }
            },
            // The login command can only be executed if both username and password fields are not empty.
            param => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));
        }
    }
}