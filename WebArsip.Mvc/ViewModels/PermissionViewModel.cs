namespace WebArsip.Mvc.Models.ViewModels
{
    public class PermissionViewModel
    {
        public int PermissionId { get; set; }
        public int RoleId { get; set; }
        public int DocId { get; set; }

        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }

        public string RoleName { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
    }
}