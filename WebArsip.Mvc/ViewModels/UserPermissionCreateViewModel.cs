namespace WebArsip.Mvc.Models.ViewModels
{
    public class UserPermissionCreateViewModel
    {
        public int UserId { get; set; }
        public int DocumentId { get; set; }
        public bool CanView { get; set; }
        public bool CanUpload { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

}