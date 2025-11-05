namespace WebArsip.Core.Entities
{
    public class UserPermission
    {
        public int Id { get; set; }
        public int DocId { get; set; }
        public string UserEmail { get; set; } = string.Empty;

        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }

        // Relasi
        public Document Document { get; set; } = null!;
    }
}