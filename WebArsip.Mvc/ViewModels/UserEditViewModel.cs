using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebArsip.Mvc.Models.ViewModels
{
    public class UserEditViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = "********";
        public int RoleId { get; set; }
        public bool IsActive { get; set; }

        public List<SelectListItem> Roles { get; set; } = new();
    }
}