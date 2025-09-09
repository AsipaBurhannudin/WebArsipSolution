using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Foreign Key ke Role
        public int RoleId { get; set; }

        // Navigasi ke Role
        public Role Role { get; set; } = null!;
    }
}
