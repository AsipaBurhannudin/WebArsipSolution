using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Input tidak valid." });
            }

            try
            {
                var client = _clientFactory.CreateClient("WebArsipApi");
                var response = await client.PostAsJsonAsync("auth/login", new
                {
                    Email = model.Email,
                    Password = model.Password
                });

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Baca pesan error dari API
                    string message = "Login gagal. Periksa email atau password Anda.";
                    try
                    {
                        var errorObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                        if (errorObj != null && errorObj.ContainsKey("message"))
                            message = errorObj["message"]?.ToString() ?? message;
                    }
                    catch { }

                    return Json(new { success = false, message });
                }

                // Deserialize respons API
                var json = System.Text.Json.JsonDocument.Parse(body);
                var data = json.RootElement.GetProperty("data");

                var token = data.GetProperty("token").GetString();
                var roleName = data.GetProperty("roleName").GetString() ?? "";
                var email = data.GetProperty("email").GetString() ?? "";
                var roleId = data.TryGetProperty("roleId", out var rId) ? rId.GetInt32() : 0;

                if (string.IsNullOrEmpty(token))
                    return Json(new { success = false, message = "Token tidak valid." });

                // ✅ Simpan ke Session
                HttpContext.Session.SetString("JWToken", token);
                HttpContext.Session.SetString("UserRole", roleName);
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("RoleId", roleId.ToString());

                // ✅ Buat ClaimsPrincipal untuk Cookie Authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim("JwtToken", token)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2)
                });

                return Json(new
                {
                    success = true,
                    message = "Login berhasil!",
                    redirectUrl = Url.Action("Index", "Dashboard")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
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
