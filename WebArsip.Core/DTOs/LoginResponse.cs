namespace WebArsip.Core.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}