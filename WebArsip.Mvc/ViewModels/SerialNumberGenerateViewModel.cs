using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebArsip.Mvc.Models.ViewModels;

public class SerialNumberGenerateViewModel
{
    public bool Success { get; set; }
    public string? Generated { get; set; }
}

