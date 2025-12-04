using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebArsip.Mvc.Models.ViewModels
{
    public class UserCreateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }

        public List<SelectListItem> Roles { get; set; } = new();
    }
}