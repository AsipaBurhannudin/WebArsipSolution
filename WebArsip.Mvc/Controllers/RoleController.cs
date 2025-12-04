using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public RoleController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private HttpClient CreateClient()
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateClient();
            var response = await client.GetAsync("role");
            if (!response.IsSuccessStatusCode)
                return View(new List<RoleViewModel>());

            var body = await response.Content.ReadAsStringAsync();
            var roles = JsonConvert.DeserializeObject<List<RoleViewModel>>(body);
            return View(roles ?? new List<RoleViewModel>());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(RoleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("role", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Gagal membuat role.");
                return View(model);
            }

            TempData["Success"] = "Role berhasil ditambahkan!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"role/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction("Index");

            var body = await response.Content.ReadAsStringAsync();
            var role = JsonConvert.DeserializeObject<RoleViewModel>(body);
            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RoleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"role/{model.RoleId}", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Gagal mengupdate role.");
                return View(model);
            }

            TempData["Success"] = "Role berhasil diperbarui!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"role/{id}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "Gagal menghapus role.";
            else
                TempData["Success"] = "Role berhasil dihapus!";

            return RedirectToAction("Index");
        }
    }
}