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
        public string? OriginalFileName { get; set; }
    }
}