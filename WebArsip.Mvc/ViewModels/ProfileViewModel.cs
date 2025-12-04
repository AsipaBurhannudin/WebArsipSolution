namespace WebArsip.Mvc.Models
{
    public class ProfileViewModel
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Role { get; set; }

        // Untuk update
        public string? NewFullName { get; set; }
        public string? NewAvatarUrl { get; set; }
    }
}