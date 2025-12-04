using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using WebArsip.Mvc.Models;
using Newtonsoft.Json;

namespace WebArsip.Mvc.Controllers
{
    public class ProfileController : Controller
    {
        private readonly HttpClient _httpClient;

        public ProfileController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("ApiClient"); // Gunakan HttpClient terdaftar
        }

        // 🔹 GET PROFILE
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("auth/profile");

            if (!response.IsSuccessStatusCode)
                return View(new ProfileViewModel());

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            var vm = new ProfileViewModel
            {
                FullName = data.fullName,
                Email = data.email,
                AvatarUrl = data.avatar,
                Role = data.role
            };

            return View(vm);
        }

        // 🔹 UPDATE PROFILE (Name + Avatar URL)
        [HttpPost]
        public async Task<IActionResult> Update(ProfileViewModel vm)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                fullName = vm.NewFullName,
                avatarUrl = vm.NewAvatarUrl
            };

            var response = await _httpClient.PutAsJsonAsync("auth/profile", body);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Profil berhasil diperbarui!";
            else
                TempData["Error"] = "Gagal memperbarui profil.";

            return RedirectToAction("Index");
        }
    }
}