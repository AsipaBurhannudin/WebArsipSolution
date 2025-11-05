using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using WebArsip.Mvc.Models.ViewModels;

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
            var client = CreateClient();
            var roleResp = await client.GetAsync("role");
            if (roleResp.IsSuccessStatusCode)
            {
                var body = await roleResp.Content.ReadAsStringAsync();
                var roles = JsonConvert.DeserializeObject<List<RoleViewModel>>(body) ?? new();
                ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName");
            }
            else
            {
                ViewBag.Roles = new SelectList(new List<RoleViewModel>());
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PermissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Data permission tidak valid.";
                return RedirectToAction(nameof(Create));
            }

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("permission", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal menambahkan permission.";
                return RedirectToAction(nameof(Create));
            }

            TempData["Success"] = "Permission berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var resp = await client.GetAsync("permission");
            if (!resp.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var body = await resp.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<PermissionViewModel>>(body) ?? new();
            var permission = list.FirstOrDefault(p => p.PermissionId == id);
            if (permission == null) return RedirectToAction(nameof(Index));

            var roleResp = await client.GetAsync("role");
            if (roleResp.IsSuccessStatusCode)
            {
                var roleBody = await roleResp.Content.ReadAsStringAsync();
                var roles = JsonConvert.DeserializeObject<List<RoleViewModel>>(roleBody) ?? new();
                ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", permission.RoleId);
            }

            return View(permission);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PermissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Data tidak valid.";
                return RedirectToAction(nameof(Edit), new { id = model.PermissionId });
            }

            var client = CreateClient();
            var payload = new
            {
                RoleId = model.RoleId,
                CanView = model.CanView,
                CanEdit = model.CanEdit,
                CanDelete = model.CanDelete,
                CanUpload = model.CanUpload,
                CanDownload = model.CanDownload
            };

            var response = await client.PutAsJsonAsync($"permission/{model.PermissionId}", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errMsg = $"Gagal memperbarui permission. (HTTP {response.StatusCode})";
                TempData["Error"] = errMsg;
                return RedirectToAction(nameof(Edit), new { id = model.PermissionId });
            }

            TempData["Success"] = "Permission berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"permission/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus permission." });

            return Json(new { success = true, message = "Permission dihapus!" });
        }
    }
}