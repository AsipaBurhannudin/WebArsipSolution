using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WebArsip.Mvc.Models.ViewModels
{
    public class DocumentBulkItemViewModel
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; }
        public IFormFile? FileUpload { get; set; }
    }

    public class DocumentBulkCreateViewModel
    {
        public List<DocumentBulkItemViewModel> Items { get; set; } = new();
    }
}