using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
        [ForeignKey("DocId")]
        public Document Document { get; set; } = null!;

    }
    public static class Features
    {
        public const string DocumentView = "Document.View";
        public const string DocumentCreate = "Document.Create";
        public const string DocumentEdit = "Document.Edit";
        public const string DocumentDelete = "Document.Delete";
        public const string DocumentDownload = "Document.Download";
    }
}
