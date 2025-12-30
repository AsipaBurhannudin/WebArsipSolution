using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebArsip.Core.DTOs;
using WebArsip.Mvc.Models.ViewModels;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WebArsip.Mvc.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DocumentController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

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

        // -------------------------
        // INDEX
        // -------------------------
        public async Task<IActionResult> Index()
        {
            var client = CreateClient();
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // permission check
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

        private async Task RefillSerialFormats(HttpClient client)
        {
            // NOTE: BaseAddress already contains "api/" — use "SerialNumber" (no leading "api/")
            var resp = await client.GetAsync("SerialNumber");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                ViewBag.SerialFormats = System.Text.Json.JsonSerializer.Deserialize<List<SerialNumberFormatDto>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<SerialNumberFormatDto>();
            }
            else
            {
                ViewBag.SerialFormats = new List<SerialNumberFormatDto>();
            }
        }

        // -------------------------
        // BULK CREATE - GET
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> BulkCreate()
        {
            var vm = new DocumentBulkCreateViewModel();
            vm.Items.Add(new DocumentBulkItemViewModel()); // default 1 row

            var client = CreateClient();
            // use "SerialNumber" (relative)
            var resp = await client.GetAsync("SerialNumber");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                ViewBag.SerialFormats = System.Text.Json.JsonSerializer.Deserialize<List<SerialNumberFormatDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<SerialNumberFormatDto>();
            }
            else
            {
                ViewBag.SerialFormats = new List<SerialNumberFormatDto>();
            }

            return View(vm);
        }

        // -------------------------
        // BULK CREATE - POST
        // Accepts optional global SerialFormatId (from form select name="SerialFormatId")
        // -------------------------
        [HttpPost]
        public async Task<IActionResult> BulkCreate(DocumentBulkCreateViewModel vm, int? SerialFormatId)
        {
            if (vm.Items == null || vm.Items.Count == 0)
            {
                TempData["Error"] = "Tidak ada data yang diinput.";
                return RedirectToAction(nameof(BulkCreate));
            }

            var client = CreateClient();
            int created = 0;

            // If a serial format selected, fetch the format key once (use "SerialNumber")
            string? selectedKey = null;
            if (SerialFormatId.HasValue)
            {
                var respList = await client.GetAsync("SerialNumber");
                if (respList.IsSuccessStatusCode)
                {
                    var listJson = await respList.Content.ReadAsStringAsync();
                    var formats = System.Text.Json.JsonSerializer.Deserialize<List<SerialNumberFormatDto>>(listJson, _jsonOpts);
                    var f = formats?.FirstOrDefault(x => x.Id == SerialFormatId.Value);
                    if (f != null) selectedKey = f.Key;
                }
            }

            string storagePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads");
            Directory.CreateDirectory(storagePath);

            foreach (var item in vm.Items)
            {
                // If global SerialFormat selected and item.Title is empty => reserve a serial for this item
                if (!string.IsNullOrWhiteSpace(selectedKey) && string.IsNullOrWhiteSpace(item.Title))
                {
                    var genBody = new { Key = selectedKey };
                    var genResp = await client.PostAsJsonAsync("SerialNumber/generate", genBody); // <-- CORRECT PATH
                    if (genResp.IsSuccessStatusCode)
                    {
                        var genResultJson = await genResp.Content.ReadAsStringAsync();
                        var genResult = System.Text.Json.JsonSerializer.Deserialize<SerialNumberGenerateResponseDto>(genResultJson, _jsonOpts);
                        if (genResult != null && !string.IsNullOrWhiteSpace(genResult.Generated))
                        {
                            item.Title = genResult.Generated;
                        }
                        else
                        {
                            // fallback: skip this item
                            continue;
                        }
                    }
                    else
                    {
                        // failed to generate serial => skip item
                        continue;
                    }
                }

                // require file to create
                if (item.FileUpload == null || item.FileUpload.Length == 0)
                    continue;

                string ext = Path.GetExtension(item.FileUpload.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };
                if (!allowedExts.Contains(ext))
                    continue;

                string fileName = $"{Guid.NewGuid()}{ext}";
                string fullPath = Path.Combine(storagePath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await item.FileUpload.CopyToAsync(stream);

                var newDoc = new DocumentViewModel
                {
                    Title = item.Title ?? "",
                    Description = item.Description ?? "",
                    Status = string.IsNullOrWhiteSpace(item.Status) ? "Published" : item.Status,
                    FilePath = fileName,
                    OriginalFileName = item.FileUpload.FileName,
                    Version = 1
                };

                var resp = await client.PostAsJsonAsync("document", newDoc);
                if (resp.IsSuccessStatusCode) created++;
            }

            TempData["Success"] = $"Berhasil menambahkan {created} dokumen.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // CREATE - GET (load preview formats)
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = CreateClient();
            // load formats (use "SerialNumber")
            var resp = await client.GetAsync("SerialNumber");

            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                try
                {
                    ViewBag.SerialFormats =
                        System.Text.Json.JsonSerializer.Deserialize<List<SerialNumberFormatDto>>(json, _jsonOpts) ?? new List<SerialNumberFormatDto>();
                }
                catch
                {
                    ViewBag.SerialFormats = new List<SerialNumberFormatDto>();
                }
            }
            else
            {
                ViewBag.SerialFormats = new List<SerialNumberFormatDto>();
            }

            return View(new DocumentViewModel());
        }

        // -------------------------
        // CREATE - POST
        // If SerialFormatId present and Title empty, server will call SerialNumber/generate
        // to reserve the serial (increment) and set model.Title before creating Document.
        // -------------------------
        [HttpPost]
        public async Task<IActionResult> Create(DocumentViewModel model, IFormFile FileUpload, int? SerialFormatId)
        {
            var client = CreateClient();

            // --- 1. Generate Serial Number (jika Title kosong & format dipilih)
            if (SerialFormatId.HasValue && string.IsNullOrWhiteSpace(model.Title))
            {
                var respList = await client.GetAsync("SerialNumber"); // <-- corrected
                if (respList.IsSuccessStatusCode)
                {
                    var listJson = await respList.Content.ReadAsStringAsync();
                    var formats = System.Text.Json.JsonSerializer.Deserialize<List<SerialNumberFormatDto>>(listJson, _jsonOpts);

                    var f = formats?.FirstOrDefault(x => x.Id == SerialFormatId.Value);
                    if (f != null)
                    {
                        var genBody = new { Key = f.Key };
                        var genResp = await client.PostAsJsonAsync("SerialNumber/generate", genBody); // <-- corrected

                        if (genResp.IsSuccessStatusCode)
                        {
                            var genJson = await genResp.Content.ReadAsStringAsync();
                            var genResult = System.Text.Json.JsonSerializer.Deserialize<SerialNumberGenerateResponseDto>(genJson, _jsonOpts);

                            if (genResult != null && !string.IsNullOrWhiteSpace(genResult.Generated))
                            {
                                model.Title = genResult.Generated;
                            }
                            else
                            {
                                TempData["Error"] = "Gagal menghasilkan serial unik.";
                                await RefillSerialFormats(client);
                                return View(model);
                            }
                        }
                        else
                        {
                            var errText = await genResp.Content.ReadAsStringAsync();
                            TempData["Error"] = $"Gagal melakukan request serial: {errText}";
                            await RefillSerialFormats(client);
                            return View(model);
                        }
                    }
                }
                else
                {
                    TempData["Error"] = "Gagal mengambil daftar serial format.";
                    await RefillSerialFormats(client);
                    return View(model);
                }
            }

            // --- 2. Validasi file wajib
            if (FileUpload == null || FileUpload.Length == 0)
            {
                TempData["Error"] = "File harus di-upload.";
                await RefillSerialFormats(client);
                return View(model);
            }

            string ext = Path.GetExtension(FileUpload.FileName).ToLowerInvariant();
            var allowed = new[] { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Format file tidak didukung.";
                await RefillSerialFormats(client);
                return View(model);
            }

            // --- 3. Simpan file ke storage
            string storagePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads");
            Directory.CreateDirectory(storagePath);

            string fileName = $"{Guid.NewGuid()}{ext}";
            string fullPath = Path.Combine(storagePath, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
                await FileUpload.CopyToAsync(fs);

            // --- 4. Kirim ke API untuk create document
            var newDoc = new DocumentViewModel
            {
                Title = model.Title ?? "",
                Description = model.Description ?? "",
                Status = string.IsNullOrWhiteSpace(model.Status) ? "Published" : model.Status,
                FilePath = fileName,
                OriginalFileName = FileUpload.FileName,
                Version = 1
            };

            var respCreate = await client.PostAsJsonAsync("document", newDoc);
            if (!respCreate.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal menyimpan dokumen.";
                await RefillSerialFormats(client);
                return View(model);
            }

            TempData["Success"] = "Dokumen berhasil dibuat.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // EDIT / DELETE / DOWNLOAD / PREVIEW - unchanged
        // -------------------------
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

                var storagePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads");
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

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"document/{id}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Gagal menghapus dokumen." });

            return Json(new { success = true, message = "Dokumen berhasil dihapus!" });
        }

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