using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using WebArsip.Core.DTOs;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    [Authorize]
    public class SerialNumberController : Controller
    {
        private readonly IHttpClientFactory _http;

        public SerialNumberController(IHttpClientFactory http)
        {
            _http = http;
        }

        // =============================
        // INDEX
        // =============================
        public async Task<IActionResult> Index()
        {
            var client = _http.CreateClient("WebArsipApi");
            var list = await client.GetFromJsonAsync<List<SerialNumberFormatDto>>("SerialNumber");

            return View(list ?? new List<SerialNumberFormatDto>());
        }

        // =============================
        // CREATE (GET)
        // =============================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =============================
        // CREATE (POST)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Create(SerialNumberCreateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var client = _http.CreateClient("WebArsipApi");

            var resp = await client.PostAsJsonAsync("SerialNumber", dto);
            var json = await resp.Content.ReadFromJsonAsync<ApiResponseViewModel>();

            if (json == null || !json.Success)
            {
                ViewBag.Error = json?.Message ?? "Create failed.";
                return View(dto);
            }

            return RedirectToAction("Index");
        }

        // =============================
        // EDIT (GET)
        // =============================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _http.CreateClient("WebArsipApi");

            var item = await client.GetFromJsonAsync<SerialNumberFormatDto>($"SerialNumber/{id}");
            if (item == null)
                return NotFound();

            return View(item);
        }

        // =============================
        // EDIT (POST)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Edit(SerialNumberFormatDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var client = _http.CreateClient("WebArsipApi");

            var resp = await client.PutAsJsonAsync($"SerialNumber/{dto.Id}", dto);
            var json = await resp.Content.ReadFromJsonAsync<ApiResponseViewModel>();

            if (json == null || !json.Success)
            {
                ModelState.AddModelError("", json?.Message ?? "Failed to edit serial format.");
                return View(dto);
            }

            return RedirectToAction("Index");
        }

        // =============================
        // DELETE
        // =============================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _http.CreateClient("WebArsipApi");

            var resp = await client.DeleteAsync($"SerialNumber/{id}");
            var json = await resp.Content.ReadFromJsonAsync<ApiResponseViewModel>();

            if (json == null || !json.Success)
                TempData["Error"] = json?.Message ?? "Delete failed.";

            return RedirectToAction("Index");
        }

        // =============================
        // GET FORMAT KEY (Auto Title)
        // =============================
        [HttpGet]
        public async Task<IActionResult> GetFormatKey(int id)
        {
            try
            {
                var client = _http.CreateClient("WebArsipApi");
                var item = await client.GetFromJsonAsync<SerialNumberFormatDto>($"SerialNumber/{id}");

                if (item == null)
                    return Json(new { success = false, message = "Format not found." });

                return Json(new
                {
                    success = true,
                    key = item.Key,
                    pattern = item.Pattern // <-- WAJIB DIKIRIM
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =============================
        // PREVIEW SERIAL
        // =============================
        [HttpPost]
        public async Task<IActionResult> PreviewSerial([FromBody] SerialNumberGenerateRequestDto req)
        {
            try
            {
                var client = _http.CreateClient("WebArsipApi");

                var resp = await client.PostAsJsonAsync("SerialNumber/preview", req);
                var body = await resp.Content.ReadFromJsonAsync<SerialNumberGenerateResponseDto>();

                if (body == null || body.Success == false)
                    return Json(new { success = false, message = "Failed to generate serial" });

                return Json(new { success = true, generated = body.Generated });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =============================
        // GENERATE SERIAL (Final Submit)
        // =============================
        [HttpPost]
        public async Task<IActionResult> GenerateSerial(string key)
        {
            try
            {
                var client = _http.CreateClient("WebArsipApi");

                var resp = await client.PostAsJsonAsync("SerialNumber/generate", new { key });
                var json = await resp.Content.ReadFromJsonAsync<SerialNumberGenerateViewModel>();

                if (json == null)
                    return Json(new { success = false });

                return Json(new { success = json.Success, generated = json.Generated });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}