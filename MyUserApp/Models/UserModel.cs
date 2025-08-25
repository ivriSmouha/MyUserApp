namespace MyUserApp.Models
{
    /// <summary>
    /// Represents a user account in the application.
    /// </summary>
    public class UserModel
    {
        /// <summary>
        /// The user's unique login name.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The user's password for authentication.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// A flag indicating whether the user has administrative privileges.
        /// </summary>
        public bool IsAdmin { get; set; }
    }
}