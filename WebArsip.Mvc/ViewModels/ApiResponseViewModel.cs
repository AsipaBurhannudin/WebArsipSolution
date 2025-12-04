using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebArsip.Mvc.Models.ViewModels
{
    public class ApiResponseViewModel
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}


public class SerialNumberGenerateVm
{
    public bool Success { get; set; }
    public string? Generated { get; set; }
}

public class ApiResponseVm
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}