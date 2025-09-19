using Microsoft.AspNetCore.Mvc;
using WebArsip.Mvc.Models;

namespace WebArsip.Mvc.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AuthController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("WebArsipApi");
            var response = await client.PostAsJsonAsync("auth/login", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Login gagal, periksa email/password.";
                return View(model);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token == null)
            {
                TempData["ErrorMessage"] = "Token tidak valid.";
                return View(model);
            }

            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("UserEmail", model.Email);

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            TempData["SuccessMessage"] = "Login berhasil, selamat datang!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Anda berhasil logout.";
            return RedirectToAction("Login", "Auth");
        }
    }
}