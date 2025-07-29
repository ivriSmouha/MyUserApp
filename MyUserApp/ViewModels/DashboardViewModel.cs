// ViewModels/DashboardViewModel.cs
using MyUserApp.Models;
using System;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    // This ViewModel is for the regular user's screen.
    public class DashboardViewModel : BaseViewModel
    {
        // A property to hold the personalized welcome message.
        public string WelcomeMessage { get; }

        // The logout command, similar to the admin view.
        public ICommand LogoutCommand { get; }
        public event Action OnLogoutRequested;

        // The constructor receives the logged-in user.
        public DashboardViewModel(UserModel user)
        {
            if (user == null) return;

            // Set the welcome message based on the user's name.
            WelcomeMessage = $"Welcome, {user.Username}!";

            // Wire up the logout command.
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
        }
    }
}