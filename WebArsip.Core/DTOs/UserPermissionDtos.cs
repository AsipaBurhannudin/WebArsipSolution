namespace WebArsip.Core.DTOs
{
    public class UserPermissionReadDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int DocId { get; set; }
        public string DocTitle { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }
    }

    public class UserPermissionCreateDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public int DocId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }
    }
}