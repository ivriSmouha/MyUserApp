// Views/LoginView.xaml.cs
using MyUserApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MyUserApp.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        // מתודה זו מעדכנת ידנית את הסיסמה ב-ViewModel בכל פעם שהיא משתנה בתיבה.
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}