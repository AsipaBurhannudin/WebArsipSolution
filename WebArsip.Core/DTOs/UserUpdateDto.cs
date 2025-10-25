using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.DTOs
{
    public class UserUpdateDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
