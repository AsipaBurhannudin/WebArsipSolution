using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.Entities
{
    public class Permission
    {
        public int PermissionId { get; set; }
        public int RoleId { get; set; }
        public int DocId { get; set; }

        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }

        // Relasi
        public Role Role { get; set; } = null!;
        public Document Document { get; set; } = null!;

    }
}
