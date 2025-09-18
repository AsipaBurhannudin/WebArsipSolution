using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using WebArsip.Mvc.DTOs;
using WebArsip.Mvc.Models;

namespace WebArsip.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel login)
        {
            if (!ModelState.IsValid)
                return View(login);

            var client = _httpClientFactory.CreateClient("API");

            // call API /api/auth/login
            var response = await client.PostAsJsonAsync("http://localhost:5287/api/auth/login",login);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Login gagal, cek email atau password.");
                return View(login);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            if (string.IsNullOrEmpty(result?.Token))
            {
                ModelState.AddModelError("", "Token tidak valid dari server.");
                return View(login);
            }

            // Simpan token di session
            HttpContext.Session.SetString("JWTToken", result.Token);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWTToken");
            return RedirectToAction("Login", "Auth");
        }
    }
}