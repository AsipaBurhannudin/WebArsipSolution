using System.ComponentModel.DataAnnotations;

namespace WebArsip.Mvc.Models.ViewModels
{
    public class RoleViewModel
    {
        public int RoleId { get; set; }     // PK dari Role (karena di Identity pakai int)

        [Required(ErrorMessage = "Role name wajib diisi.")]
        [StringLength(50, ErrorMessage = "Role name maksimal 50 karakter.")]
        public string RoleName { get; set; } = string.Empty; // Nama role, misalnya "Admin", "Compliance"
    }
}