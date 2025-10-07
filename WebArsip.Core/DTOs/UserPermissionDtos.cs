namespace WebArsip.Core.DTOs
{
    public class UserPermissionQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public int? UserId { get; set; }
        public int? DocId { get; set; }
        public bool? CanView { get; set; }
    }

    public class UserPermissionReadDto
    {
        public int UserPermissionId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public int DocId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;

        public bool CanView { get; set; }
        public bool CanUpload { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class UserPermissionCreateDto
    {
        public int UserId { get; set; }
        public int DocId { get; set; }
        public bool CanView { get; set; } = false;
        public bool CanUpload { get; set; } = false;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
    }

    public class UserPermissionUpdateDto : UserPermissionCreateDto
    {
        // same fields; included for clarity
    }

}