using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public DocumentController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // 🔹 Helper untuk membuat HttpClient dengan JWT
        private HttpClient CreateClient()
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // 🔹 List dokumen
        public async Task<IActionResult> Index()
        {
            var client = CreateClient();
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

        // 🔹 Create GET
        [HttpGet]
        public IActionResult Create() => View();

        // 🔹 Create POST
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

        // 🔹 Detail Dokumen
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/{id}");
            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonConvert.DeserializeObject<DocumentViewModel>(body);
            return View(doc);
        }

        // 🔹 Edit GET
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

        // 🔹 Edit POST
        [HttpPost]
        public async Task<IActionResult> Edit(DocumentViewModel model, IFormFile? FileUpload)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateClient();

            // Ambil data lama dari API
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
                // 🔹 Tidak upload baru → tetap pakai file lama
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

        // 🔹 Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"document/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus dokumen." });

            return Json(new { success = true, message = "Dokumen berhasil dihapus!" });
        }

        // 🔹 Download
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/stream/{id}");

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

        // 🔹 Preview (tampilkan langsung)
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/stream/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/pdf";

            // 🔹 tampilkan langsung di iframe
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(stream, contentType);
        }

        // 🔹 Model pagination helper
        public class PagedResult<T>
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public List<T> Items { get; set; } = new();
        }
    }
}