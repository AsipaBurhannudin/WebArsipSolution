namespace WebArsip.Mvc.Models.ViewModels
{
    public class SerialNumberPreviewViewModel
    {
        public bool Success { get; set; }
        public string? Generated { get; set; }
        public string? ErrorMessage { get; set; }

        // Pattern Key yang dipilih admin
        public string? Key { get; set; }

        // Tanggal dokumen (penting untuk roman month & reset counter)
        public DateTime? DocumentDate { get; set; }
    }
}