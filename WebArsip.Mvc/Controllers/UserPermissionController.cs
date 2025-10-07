using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
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
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        // GET: /UserPermission
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"/api/UserPermission?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal memuat data UserPermission.";
                return View(new List<UserPermissionViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            var paged = JsonConvert.DeserializeObject<PagedResult<UserPermissionViewModel>>(body);

            return View(paged?.Items ?? new List<UserPermissionViewModel>());
        }

        // GET: /UserPermission/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: /UserPermission/Create
        [HttpPost]
        public async Task<IActionResult> Create(UserPermissionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("UserPermission", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal menambahkan UserPermission.";
                await PopulateDropdowns();
                return View(model);
            }

            TempData["Success"] = "UserPermission berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /UserPermission/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await PopulateDropdowns();

            var client = CreateClient();
            var response = await client.GetAsync($"UserPermission/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var body = await response.Content.ReadAsStringAsync();
            var up = JsonConvert.DeserializeObject<UserPermissionEditViewModel>(body);

            return View(up);
        }

        // POST: /UserPermission/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(UserPermissionEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"UserPermission/{model.UserPermissionId}", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mengedit UserPermission.";
                await PopulateDropdowns();
                return View(model);
            }

            TempData["Success"] = "UserPermission berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserPermission/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"UserPermission/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus UserPermission." });

            return Json(new { success = true, message = "UserPermission berhasil dihapus!" });
        }

        // Helper untuk dropdown (User + Document)
        private async Task PopulateDropdowns()
        {
            var client = CreateClient();

            // Get users
            var userResp = await client.GetAsync("User?page=1&pageSize=100");
            var users = new List<UserViewModel>();
            if (userResp.IsSuccessStatusCode)
            {
                var body = await userResp.Content.ReadAsStringAsync();
                var paged = JsonConvert.DeserializeObject<PagedResult<UserViewModel>>(body);
                users = paged?.Items ?? new List<UserViewModel>();
            }
            ViewBag.Users = users.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.Name} ({u.Email})"
            }).ToList();

            // Get documents
            var docResp = await client.GetAsync("Document?page=1&pageSize=100");
            var docs = new List<DocumentViewModel>();
            if (docResp.IsSuccessStatusCode)
            {
                var body = await docResp.Content.ReadAsStringAsync();
                var pagedDocs = JsonConvert.DeserializeObject<PagedResult<DocumentViewModel>>(body);
                docs = pagedDocs?.Items ?? new();
            }
            ViewBag.Documents = docs.Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = d.DocId.ToString(),
                Text = d.Title
            }).ToList();
        }

        // PagedResult generic
        public class PagedResult<T>
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public List<T> Items { get; set; } = new();
        }
    }
}