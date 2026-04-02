namespace Final_Backend_API.Models
{
    // ══════════════════════════════════════
    // Signup Model
    // ══════════════════════════════════════
    public class SignupModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Dob { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Confirm { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════
    // Login Model
    // ══════════════════════════════════════
    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}