using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DocumentController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public DocumentController(IHttpClientFactory clientFactory)
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

        // 📄 INDEX — List Document + Check Permission
        public async Task<IActionResult> Index()
        {
            var client = CreateClient();

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // 🔹 Ambil permission user dari API
            var permResponse = await client.GetAsync($"permission/check?email={email}&role={role}");
            if (permResponse.IsSuccessStatusCode)
            {
                var permJson = await permResponse.Content.ReadAsStringAsync();
                dynamic p = JsonConvert.DeserializeObject(permJson)!;
                ViewBag.CanView = (bool?)p?.CanView ?? false;
                ViewBag.CanEdit = (bool?)p?.CanEdit ?? false;
                ViewBag.CanDelete = (bool?)p?.CanDelete ?? false;
                ViewBag.CanUpload = (bool?)p?.CanUpload ?? false;
                ViewBag.CanDownload = (bool?)p?.CanDownload ?? false;
            }
            else
            {
                ViewBag.CanView = ViewBag.CanEdit = ViewBag.CanDelete = ViewBag.CanUpload = ViewBag.CanDownload = false;
            }

            // 🔹 Ambil data dokumen
            var response = await client.GetAsync("document?page=1&pageSize=100");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mengambil data dokumen.";
                return View(new List<DocumentViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            var paged = JsonConvert.DeserializeObject<PagedResult<DocumentViewModel>>(body);
            return View(paged?.Items ?? new List<DocumentViewModel>());
        }

        // ➕ CREATE — GET
        [HttpGet]
        public IActionResult Create() => View();

        // ➕ CREATE — POST
        [HttpPost]
        public async Task<IActionResult> Create(DocumentViewModel model, IFormFile FileUpload)
        {
            if (!ModelState.IsValid) return View(model);

            if (FileUpload != null && FileUpload.Length > 0)
            {
                var ext = Path.GetExtension(FileUpload.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

                if (!allowedExts.Contains(ext))
                {
                    TempData["Error"] = "Hanya file Word, Excel, atau PDF yang diperbolehkan.";
                    return RedirectToAction(nameof(Create));
                }

                var storagePath = Path.Combine(
                    Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                    "WebArsipStorage", "uploads");
                Directory.CreateDirectory(storagePath);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(storagePath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await FileUpload.CopyToAsync(stream);

                model.FilePath = fileName;
                model.OriginalFileName = FileUpload.FileName;
            }

            model.Status ??= "Published";
            model.Version = 1;

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("document", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal menambahkan dokumen.";
                return View(model);
            }

            TempData["Success"] = "Dokumen berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        // ✏️ EDIT — GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonConvert.DeserializeObject<DocumentViewModel>(body);
            return View(doc);
        }

        // ✏️ EDIT — POST
        [HttpPost]
        public async Task<IActionResult> Edit(DocumentViewModel model, IFormFile? FileUpload)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateClient();

            DocumentViewModel? oldDoc = null;
            var existingResponse = await client.GetAsync($"document/{model.DocId}");
            if (existingResponse.IsSuccessStatusCode)
            {
                var oldData = await existingResponse.Content.ReadAsStringAsync();
                oldDoc = JsonConvert.DeserializeObject<DocumentViewModel>(oldData);
            }

            if (FileUpload != null && FileUpload.Length > 0)
            {
                var ext = Path.GetExtension(FileUpload.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

                if (!allowedExts.Contains(ext))
                {
                    TempData["Error"] = "Hanya file Word, Excel, atau PDF yang diperbolehkan.";
                    return RedirectToAction(nameof(Edit), new { id = model.DocId });
                }

                var storagePath = Path.Combine(
                    Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                    "WebArsipStorage", "uploads");
                Directory.CreateDirectory(storagePath);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(storagePath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await FileUpload.CopyToAsync(stream);

                model.FilePath = fileName;
                model.OriginalFileName = FileUpload.FileName;
            }
            else if (oldDoc != null)
            {
                model.FilePath = oldDoc.FilePath;
                model.OriginalFileName = oldDoc.OriginalFileName;
            }

            var response = await client.PutAsJsonAsync($"document/{model.DocId}", model);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal memperbarui dokumen.";
                return View(model);
            }

            TempData["Success"] = "Dokumen berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        // 🗑 DELETE
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"document/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus dokumen." });

            return Json(new { success = true, message = "Dokumen berhasil dihapus!" });
        }

        // ⬇ DOWNLOAD
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/download/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "File tidak ditemukan di server.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            return File(stream, contentType, fileName);
        }

        // 👁 PREVIEW
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/preview/{id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Tidak memiliki izin untuk melihat dokumen ini.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/pdf";

            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(stream, contentType);
        }

        // Helper class
        public class PagedResult<T>
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public List<T> Items { get; set; } = new();
        }
    }
}