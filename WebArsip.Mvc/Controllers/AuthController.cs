using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebArsip.Mvc.Models.ViewModels;

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
                TempData["ErrorMessage"] = "Login gagal, periksa email/password.";
                return View(model);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token == null)
            {
                TempData["ErrorMessage"] = "Token tidak valid.";
                return View(model);
            }

            // 🔹 Simpan ke Session
            HttpContext.Session.SetString("RoleId", result.RoleId.ToString());
            HttpContext.Session.SetString("UserRole", result.RoleName?.Trim() ?? "");
            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("UserEmail", model.Email);

            // 🔹 Buat ClaimsPrincipal untuk Cookie Authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Email),
                new Claim(ClaimTypes.Role, result.RoleName ?? ""), // penting!
                new Claim("JwtToken", result.Token)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            });

            // 🔹 Redirect
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            TempData["SuccessMessage"] = "Login berhasil, selamat datang!";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");

            TempData["SuccessMessage"] = "Anda berhasil logout.";
            return RedirectToAction("Login", "Auth");
        }
    }
}