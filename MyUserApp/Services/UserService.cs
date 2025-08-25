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
        public const string DeletedUserUsername = "[Deleted User]";

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
                var usersFromFile = JsonSerializer.Deserialize<ObservableCollection<UserModel>>(json) ?? new ObservableCollection<UserModel>();
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

            EnsureSpecialUsersExist();
        }

        private void EnsureSpecialUsersExist()
        {
            // Ensure the special "[Deleted User]" account exists.
            if (!Users.Any(u => u.Username == DeletedUserUsername))
            {
                Users.Add(new UserModel { Username = DeletedUserUsername, Password = "000", IsAdmin = false });
                SaveUsers();
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
            if (Users.Any(u => u.Username.Equals(newUser.Username, System.StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Username already exists.");
                return;
            }

            if (newUser.Username == DeletedUserUsername)
            {
                MessageBox.Show($"'{DeletedUserUsername}' is a reserved username and cannot be created.", "Reserved Username");
                return;
            }
            Users.Add(newUser);
            SaveUsers();
        }


        /// <summary>
        /// Deletes a user after reassigning their projects to the special "[Deleted User]" account.
        /// </summary>
        public void DeleteUserAndReassignProjects(UserModel userToDelete)
        {
            if (userToDelete == null || userToDelete.Username == "admin" || userToDelete.Username == DeletedUserUsername)
            {
                // This is a safety check. The UI should prevent this.
                MessageBox.Show("This user cannot be deleted.", "Deletion Not Allowed");
                return;
            }

            // Step 1: Reassign projects.
            ReportService.Instance.ReassignProjects(userToDelete.Username, DeletedUserUsername);

            // Step 2: Remove the user from the collection.
            Users.Remove(userToDelete);

            // Step 3: Save the updated user list.
            SaveUsers();
        }
    }
}