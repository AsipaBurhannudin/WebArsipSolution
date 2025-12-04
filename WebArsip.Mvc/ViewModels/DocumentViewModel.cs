namespace WebArsip.Mvc.Models.ViewModels
{
    public class DocumentViewModel
    {
        public int DocId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public string? OriginalFileName { get; set; }
        public string? CreatedBy { get; set; }
    }
}