using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebArsip.Mvc.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebArsip.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Azure;

namespace WebArsip.Mvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public UserController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private HttpClient CreateClient()
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        private async Task<List<SelectListItem>> GetRoles()
        {
            var client = CreateClient();
            var response = await client.GetAsync("role");

            if (!response.IsSuccessStatusCode) return new List<SelectListItem>();

            var body = await response.Content.ReadAsStringAsync();
            var roles = JsonConvert.DeserializeObject<List<RoleReadDto>>(body); // DTO sesuai API kamu

            return roles.Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleName
            }).ToList();
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"user?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mengambil data user.";
                return View(new List<UserViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            var paged = JsonConvert.DeserializeObject<PagedResult<UserViewModel>>(body);

            return View(paged?.Items ?? new List<UserViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"user/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mengambil detail user.";
                return RedirectToAction(nameof(Index));
            }

            var body = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(body);

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserCreateViewModel
            {
                Roles = await GetRoles()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = await GetRoles();
                return View(model);
            }

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("user", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal menambahkan user.";
                model.Roles = await GetRoles();
                return View(model);
            }

            TempData["Success"] = "User berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"user/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var body = await response.Content.ReadAsStringAsync();
            var userData = JsonConvert.DeserializeObject<UserViewModel>(body);

            var allRoles = await GetRoles();

            // Temukan RoleId berdasarkan nama role user
            var selectedRole = allRoles.FirstOrDefault(r =>
                string.Equals(r.Text, userData.RoleName, StringComparison.OrdinalIgnoreCase))?.Value;

            var model = new UserEditViewModel
            {
                UserId = userData.UserId,
                Name = userData.Name,
                Email = userData.Email,
                Password = "********",
                RoleId = int.TryParse(selectedRole, out int rid) ? rid : 0,
                Roles = allRoles,
                IsActive = userData.IsActive
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = await GetRoles();
                return View(model);
            }

            var client = CreateClient();

            // Hapus password dummy agar tidak terkirim
            model.Password = null;

            var response = await client.PutAsJsonAsync($"user/{model.UserId}", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mengedit user.";
                model.Roles = await GetRoles();
                return View(model);
            }

            TempData["Success"] = "Perubahan berhasil disimpan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"user/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus user." });

            return Json(new { success = true, message = "User berhasil dihapus!" });
        }

        [HttpGet]
        public IActionResult ResetPassword(int id)
        {
            return View(new ResetPasswordViewModel { UserId = id });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                $"user/reset-password/{model.UserId}",
                new { NewPassword = model.NewPassword }
            );

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mereset password!";
                return View(model);
            }

            TempData["Success"] = "Password berhasil direset!";
            return RedirectToAction(nameof(Index));
        }

        public class PagedResult<T>
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public List<T> Items { get; set; } = new();
        }
    }
}