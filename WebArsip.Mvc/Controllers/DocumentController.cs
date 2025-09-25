using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebArsip.Mvc.ViewModels;

namespace WebArsip.Mvc.Controllers
{
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

        public async Task<IActionResult> Index()
        {
            var client = CreateClient();
            var response = await client.GetAsync("document?page=1&pageSize=100"); // ambil banyak data dulu
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Gagal mengambil data dokumen.";
                return View(new List<DocumentViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            var paged = JsonConvert.DeserializeObject<PagedResult<DocumentViewModel>>(body);

            return View(paged?.Items ?? new List<DocumentViewModel>());
        }

        // 👁 GET: Document/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonConvert.DeserializeObject<DocumentViewModel>(body)!;
            return View(doc);
        }

        /*[HttpGet]
        public IActionResult Create() => View();
        

        [HttpPost]
        public async Task<IActionResult> Create(DocumentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("document", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Gagal menambahkan dokumen");
                return View(model);
            }

            TempData["SuccessMessage"] = "Dokumen berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }*/

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(DocumentViewModel model, IFormFile FileUpload)
        {
            if (!ModelState.IsValid) return View(model);

            if (FileUpload != null && FileUpload.Length > 0)
            {
                var ext = Path.GetExtension(FileUpload.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".doc", ".docx", ".xls", ".xlsx" };
                if (!allowedExts.Contains(ext))
                {
                    ModelState.AddModelError("", "Hanya file Word atau Excel yang diperbolehkan.");
                    return View(model);
                }

                // Simpan file ke folder wwwroot/uploads
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", FileUpload.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await FileUpload.CopyToAsync(stream);
                }

                model.FilePath = "/uploads/" + FileUpload.FileName;
            }

            // Kirim data ke API
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsJsonAsync("document", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Gagal menyimpan dokumen.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Dokumen berhasil ditambahkan!";
            return RedirectToAction("Index");
        }

        /*[HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClient();
            var response = await client.GetAsync($"document/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonConvert.DeserializeObject<DocumentViewModel>(body);

            return View(doc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, DocumentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"document/{id}", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Gagal memperbarui dokumen");
                return View(model);
            }

            TempData["SuccessMessage"] = "Dokumen berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }*/

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"document/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction("Index");

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonConvert.DeserializeObject<DocumentViewModel>(body);
            return View(doc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DocumentViewModel model, IFormFile? FileUpload)
        {
            if (!ModelState.IsValid) return View(model);

            if (FileUpload != null && FileUpload.Length > 0)
            {
                var ext = Path.GetExtension(FileUpload.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".doc", ".docx", ".xls", ".xlsx" };
                if (!allowedExts.Contains(ext))
                {
                    ModelState.AddModelError("", "Hanya file Word atau Excel yang diperbolehkan.");
                    return View(model);
                }

                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await FileUpload.CopyToAsync(stream);
                }

                model.FilePath = "/uploads/" + fileName;
            }

            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"document/{model.DocId}", model);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Gagal memperbarui dokumen.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Dokumen berhasil diperbarui!";
            return RedirectToAction("Index");
        }

        /*[HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"document/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Gagal menghapus dokumen.";
            }
            else
            {
                TempData["SuccessMessage"] = "Dokumen berhasil dihapus!";
            }

            return RedirectToAction(nameof(Index));
        }*/

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"document/{id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Gagal menghapus dokumen.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Dokumen berhasil dihapus!";
            return RedirectToAction("Index");
        }

    }

    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<T> Items { get; set; } = new();
    }
}