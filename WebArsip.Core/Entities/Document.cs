namespace WebArsip.Core.Entities
{
    public class Document
    {
        public int DocId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = "Active";

        public ICollection<Archive> Archives { get; set; } = new List<Archive>();
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}