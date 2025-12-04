using WebArsip.Core.Entities;

public class Document
{
    public int DocId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Active";
    public string? OriginalFileName { get; set; }
    public string? CreatedBy { get; set; }

    public ICollection<Archive> Archives { get; set; } = new List<Archive>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}