using Microsoft.AspNetCore.Mvc;
using WebArsip.Mvc.Models;

namespace WebArsip.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AuthController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("WebArsipApi");
            var response = await client.PostAsJsonAsync("auth/login", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Login gagal, periksa email/password.");
                return View(model);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token == null)
            {
                ModelState.AddModelError("", "Token tidak valid.");
                return View(model);
            }

            // Simpan token ke session
            HttpContext.Session.SetString("JWToken", result.Token);

            // Redirect sesuai returnUrl kalau ada
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            // Default ke Home
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWToken");
            return RedirectToAction("Login", "Auth");
        }
    }
}