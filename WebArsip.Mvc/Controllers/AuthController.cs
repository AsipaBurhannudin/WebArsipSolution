using Microsoft.AspNetCore.Mvc;
using WebArsip.Core.Entities;
using WebArsip.Mvc.Models;
using WebArsip.Mvc.Helpers;
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
        public IActionResult Login()
        {
            return View();
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

            HttpContext.Session.SetString("RoleId", result.RoleId.ToString());
            HttpContext.Session.SetString("UserRole", result.RoleName);
            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("UserEmail", model.Email);

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            TempData["SuccessMessage"] = "Login berhasil, selamat datang!";
            return RedirectToAction("Index", "Dashboard");
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
