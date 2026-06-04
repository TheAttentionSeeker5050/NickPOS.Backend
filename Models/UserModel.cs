namespace NickPOS.Backend.Models
{
    public class UserModel
    {
        public long Id {get; set;}
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? PasswordHash { get; set; }
    }

    public static class AppRoles
    {
        public const string User = "User";
        public const string Manager = "Manager";
        public const string Admin = "Admin";
    }
}