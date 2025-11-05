using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    public class AuditLogController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AuditLogController(IHttpClientFactory clientFactory)
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

        // 🕓 Index Page
        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, [FromQuery(Name = "action")] string? filterAction = null)
        {
            var client = CreateClient();

            var query = "auditlog?page=1&pageSize=200";
            if (from.HasValue)
                query += $"&from={from.Value:yyyy-MM-ddTHH:mm:ss}";
            if (to.HasValue)
                query += $"&to={to.Value:yyyy-MM-ddTHH:mm:ss}";
            if (!string.IsNullOrEmpty(filterAction))
                query += $"&action={filterAction}";

            var response = await client.GetAsync(query);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = $"Gagal memuat audit logs. Status: {response.StatusCode}";
                return View(new List<AuditLogViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var pagedResult = JsonConvert.DeserializeObject<PagedResult<AuditLogViewModel>>(json);

            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.SelectedAction = filterAction;

            return View(pagedResult?.Items ?? new List<AuditLogViewModel>());
        }

        // 🔧 Helper class
        public class PagedResult<T>
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public List<T> Items { get; set; } = new();
        }
    }
}