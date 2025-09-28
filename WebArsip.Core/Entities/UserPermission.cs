using System.ComponentModel.DataAnnotations.Schema;

namespace WebArsip.Core.Entities
{
    public class UserPermission
    {
        public int UserPermissionId { get; set; }
        public int UserId { get; set; }
        public int DocId { get; set; }

        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }

        // Relasi
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        [ForeignKey("DocId")]
        public Document Document { get; set; } = null!;
    }
}