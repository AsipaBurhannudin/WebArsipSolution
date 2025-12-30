namespace WebArsip.Mvc.Models.ViewModels
{
    public class SerialNumberGenerateViewModel
    {
        public bool Success { get; set; }
        public string? Generated { get; set; }
        public long UsedNumber { get; set; }
        public string? ErrorMessage { get; set; }

        // Pattern Key
        public string? Key { get; set; }

        // Date yang menentukan RomanMonth-Year dan reset counter
        public DateTime? DocumentDate { get; set; }
    }
}