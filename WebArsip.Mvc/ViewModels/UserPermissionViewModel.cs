using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebArsip.Mvc.Models.ViewModels
{
    public class UserPermissionViewModel
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int DocId { get; set; }
        public string? DocTitle { get; set; }

        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanUpload { get; set; }
        public bool CanDownload { get; set; }

        // Dropdown sources
        public List<SelectListItem>? UserList { get; set; }
        public List<SelectListItem>? DocumentList { get; set; }
    }
}