using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MyUserApp.Models;
using MyUserApp.Services;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// A base class for all ViewModels in the application.
    /// It implements the INotifyPropertyChanged interface to allow two-way data binding
    /// between the ViewModel and the View.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event that is raised when a property value changes.
        /// The View listens to this event to know when to update its display.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event to notify the UI of a property update.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.
        /// This is automatically filled by the compiler thanks to the [CallerMemberName] attribute.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Authenticates a user against the central UserService.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <param name="password">The password to check.</param>
        /// <returns>A UserModel if credentials are valid, otherwise null.</returns>
        private UserModel AuthenticateUser(string username, string password)
        {
            return UserService.Instance.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}