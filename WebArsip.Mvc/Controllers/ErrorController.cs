using Microsoft.AspNetCore.Mvc;

namespace WebArsip.Mvc.Controllers
{
    public class ErrorController : Controller
{
    [HttpGet("Error/401")]
    public IActionResult UnauthorizedPage() => View("Unauthorized");

    [HttpGet("Error/403")]
    public IActionResult ForbiddenPage() => View("Forbidden");

    [HttpGet("Error/404")]
    public IActionResult NotFoundPage() => View("NotFound");

    [HttpGet("Error/500")]
    public IActionResult InternalServerErrorPage() => View("InternalServerError");
    }
}
