using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using WebArsip.Mvc.Models.ViewModels;
using static WebArsip.Mvc.Controllers.DocumentController;

namespace WebArsip.Mvc.Controllers
{
    public class PermissionController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public PermissionController(IHttpClientFactory clientFactory)
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
            var response = await client.GetAsync("permission");
            if (!response.IsSuccessStatusCode)
                return View(new List<PermissionViewModel>());

            var body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<PermissionViewModel>>(body);

            return View(data ?? new List<PermissionViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PermissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("permission", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal menambahkan permission.";
                await LoadDropdowns();
                return View(model);
            }

            TempData["Success"] = "Permission berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"permission/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<PermissionViewModel>(body);

            await LoadDropdowns();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PermissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"permission/{model.PermissionId}", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal update permission.";
                await LoadDropdowns();
                return View(model);
            }

            TempData["Success"] = "Permission berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            var client = CreateClient();

            var roleResp = await client.GetAsync("role");
            if (roleResp.IsSuccessStatusCode)
            {
                var body = await roleResp.Content.ReadAsStringAsync();
                var roles = JsonConvert.DeserializeObject<List<RoleViewModel>>(body) ?? new();
                ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName");
            }

            var docResp = await client.GetAsync("document?page=1&pageSize=100");
            if (docResp.IsSuccessStatusCode)
            {
                var body = await docResp.Content.ReadAsStringAsync();
                var docs = JsonConvert.DeserializeObject<PagedResult<DocumentViewModel>>(body)?.Items ?? new();
                ViewBag.Documents = new SelectList(docs, "DocId", "Title");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"permission/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus permission." });

            return Json(new { success = true, message = "Permission berhasil dihapus!" });
        }
    }
}