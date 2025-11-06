namespace WebArsip.Core.DTOs
{
    public class DocumentReadDto
    {
        public int DocId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = "Active";
        public int Version { get; set; }
        public string? OriginalFileName { get; set; }
        public string? CreatedBy { get; set; }
    }
}
