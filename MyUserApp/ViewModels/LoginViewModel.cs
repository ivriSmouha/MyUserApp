// ViewModels/LoginViewModel.cs
using MyUserApp.Models;
using System;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    // ה-ViewModel של מסך הכניסה. הוא אחראי על לוגיקת הכניסה.
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        // Action זה הוא כמו "אירוע" שמודיע שהכניסה הצליחה.
        public Action<UserModel> OnLoginSuccess { get; set; }

        // הבנאי מקבל פונקציה מבחוץ שיודעת איך לבצע אימות.
        public LoginViewModel(Func<string, string, UserModel> authenticateUser)
        {
            LoginCommand = new RelayCommand(param =>
            {
                UserModel user = authenticateUser(Username, Password);
                if (user != null)
                {
                    OnLoginSuccess?.Invoke(user); // מפעיל את אירוע ההצלחה.
                }
                else
                {
                    System.Windows.MessageBox.Show("Invalid username or password.");
                }
            }, param => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));
        }
    }
}