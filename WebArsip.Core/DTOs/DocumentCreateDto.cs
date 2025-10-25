namespace WebArsip.Core.DTOs
{
    public class DocumentCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int Version { get; set; }
        public string? OriginalFileName {  get; set; } = string.Empty;
    }
}
