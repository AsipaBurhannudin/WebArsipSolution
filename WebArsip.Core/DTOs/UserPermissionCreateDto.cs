public class UserPermissionCreateDto
{
    public int DocId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDownload { get; set; }
    public bool CanUpload { get; set; }
}