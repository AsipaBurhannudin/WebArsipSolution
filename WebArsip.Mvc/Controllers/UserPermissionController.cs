using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    public class UserPermissionController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public UserPermissionController(IHttpClientFactory clientFactory)
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

        // ✅ Index Page (list all user permissions)
        public async Task<IActionResult> Index()
        {
            var client = CreateClient();
            var response = await client.GetAsync("userpermission");
            if (!response.IsSuccessStatusCode)
                return View(new List<UserPermissionViewModel>());

            var body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<UserPermissionViewModel>>(body);
            return View(data ?? new List<UserPermissionViewModel>());
        }

        // ✅ Create (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = CreateClient();

            // ---- Get Users ----
            var userResp = await client.GetAsync("user");
            var users = new List<UserViewModel>();

            if (userResp.IsSuccessStatusCode)
            {
                var json = await userResp.Content.ReadAsStringAsync();
                users = TryExtractList<UserViewModel>(json);
            }

            ViewBag.Users = users.Select(u => new
            {
                Email = u.Email,
                DisplayName = string.IsNullOrEmpty(u.Name)
                    ? u.Email
                    : $"{u.Name} ({u.Email})"
            }).ToList();

            // ---- Get Documents ----
            var docResp = await client.GetAsync("document");
            var docs = new List<DocumentViewModel>();

            if (docResp.IsSuccessStatusCode)
            {
                var json = await docResp.Content.ReadAsStringAsync();
                docs = TryExtractList<DocumentViewModel>(json);
            }

            ViewBag.Documents = docs;

            return View();
        }

        // ✅ Create (POST)
        [HttpPost]
        public async Task<IActionResult> Create(UserPermissionViewModel model)
        {
            var client = CreateClient();

            var payload = new
            {
                UserEmail = model.UserEmail,
                DocId = model.DocId,
                CanView = model.CanView,
                CanEdit = model.CanEdit,
                CanDelete = model.CanDelete,
                CanUpload = model.CanUpload,
                CanDownload = model.CanDownload
            };

            var response = await client.PostAsJsonAsync("userpermission", payload);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorUP"] = "Gagal menambahkan User Permission. Pastikan kombinasi user dan dokumen belum ada.";
                return RedirectToAction(nameof(Create));
            }

            TempData["SuccessUP"] = "User Permission berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Edit (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"userpermission");
            if (!response.IsSuccessStatusCode) return NotFound();

            var body = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<UserPermissionViewModel>>(body);
            var model = list?.FirstOrDefault(p => p.Id == id);

            if (model == null) return NotFound();
            return View(model);
        }

        // ✅ Edit (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(UserPermissionViewModel model)
        {
            var client = CreateClient();
            var payload = new
            {
                model.DocId,
                model.UserEmail,
                model.CanView,
                model.CanEdit,
                model.CanDelete,
                model.CanUpload,
                model.CanDownload
            };

            var response = await client.PutAsJsonAsync($"userpermission/{model.Id}", payload);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorUP"] = "Gagal memperbarui permission.";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }

            TempData["SuccessUP"] = "User Permission berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }


        // ✅ Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"userpermission/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus user permission." });

            return Json(new { success = true, message = "User permission berhasil dihapus!" });
        }

        // 🔧 Utility untuk ekstraksi array dari JSON (paged atau tidak)
        private static List<T> TryExtractList<T>(string json)
        {
            try
            {
                if (json.TrimStart().StartsWith("["))
                {
                    // langsung array
                    return JsonConvert.DeserializeObject<List<T>>(json) ?? new();
                }

                var obj = JObject.Parse(json);
                if (obj["items"] != null && obj["items"].Type == JTokenType.Array)
                {
                    return obj["items"]!.ToObject<List<T>>() ?? new();
                }
            }
            catch
            {
                // fallback aman
            }

            return new List<T>();
        }
    }
}