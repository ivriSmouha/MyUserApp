using MyUserApp.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace MyUserApp.Services
{
    // This singleton service is the single source of truth for all user data.
    public class UserService
    {
        private const string FilePath = "users.json";

        // --- Singleton Pattern ---
        private static readonly UserService _instance = new UserService();
        public static UserService Instance => _instance;
        // --- End Singleton ---

        // The list of users is an ObservableCollection so that the UI can update automatically
        // anywhere it is used (e.g., in the admin panel).
        public ObservableCollection<UserModel> Users { get; private set; }

        // The private constructor ensures only one instance can be created.
        private UserService()
        {
            LoadUsers();
        }

        private void LoadUsers()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                // The deserialized list is used to create the ObservableCollection.
                var usersFromFile = JsonSerializer.Deserialize<ObservableCollection<UserModel>>(json);
                Users = new ObservableCollection<UserModel>(usersFromFile);
            }
            else
            {
                // If no file exists, create the default admin user.
                Users = new ObservableCollection<UserModel>
                {
                    new UserModel { Username = "admin", Password = "123", IsAdmin = true }
                };
            }
        }

        private void SaveUsers()
        {
            var json = JsonSerializer.Serialize(Users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        // The logic for adding a user now lives here.
        public void AddUser(UserModel newUser)
        {
            if (Users.Any(u => u.Username == newUser.Username))
            {
                MessageBox.Show("Username already exists.");
                return;
            }
            Users.Add(newUser);
            SaveUsers();
        }
    }
}