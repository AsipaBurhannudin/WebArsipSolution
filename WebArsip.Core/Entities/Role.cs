using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.Entities
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Relasi: satu Role bisa punya banyak User
        public ICollection<User>? Users { get; set; }

        // Relasi: satu Role bisa punya banyak Permission
        public ICollection<Permission>? Permissions { get; set; }
    }
}
