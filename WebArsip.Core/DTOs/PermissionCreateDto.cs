namespace WebArsip.Core.DTOs
{
    public class PermissionCreateDto
    {
        public int RoleId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }
    }
}