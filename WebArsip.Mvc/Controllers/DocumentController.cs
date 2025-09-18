using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebArsip.Mvc.Models;

namespace WebArsip.Mvc.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public DocumentController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

            var client = _clientFactory.CreateClient("WebArsipApi");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("document");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Gagal mengambil data dokumen.";
                return View(new List<DocumentViewModel>());
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var docs = JsonConvert.DeserializeObject<List<DocumentViewModel>>(responseBody)!;

            return View(docs);
        }
    }
}