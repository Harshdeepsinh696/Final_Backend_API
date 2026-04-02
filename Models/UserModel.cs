namespace Final_Backend_API.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        // ✅ REMOVED Password and IsReminderOn
        // These columns don't exist in your Users table
    }
}