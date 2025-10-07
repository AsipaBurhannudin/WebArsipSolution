namespace WebArsip.Mvc.Models.ViewModels
{
    public class UserPermissionViewModel
    {
        public int UserPermissionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanUpload { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}