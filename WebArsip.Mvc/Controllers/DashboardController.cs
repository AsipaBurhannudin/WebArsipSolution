using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebArsip.Mvc.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public DashboardController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _clientFactory.CreateClient("WebArsipApi");

            // --- Ambil Document Count ---
            var docCount = await GetCountAsync(client, "document/count");

            // --- Ambil User Count ---
            var userCount = await GetCountAsync(client, "user/count");

            // --- Ambil AuditLog Count ---
            var logCount = await GetCountAsync(client, "auditlog/count");

            // --- Ambil daily stats untuk chart ---
            var stats = new List<AuditLogStatsViewModel>();
            var statsResponse = await client.GetAsync("auditlog/daily-stats?days=7");
            if (statsResponse.IsSuccessStatusCode)
            {
                var body = await statsResponse.Content.ReadAsStringAsync();
                stats = JsonConvert.DeserializeObject<List<AuditLogStatsViewModel>>(body) ?? new List<AuditLogStatsViewModel>();
            }

            var dashboard = new DashboardViewModel
            {
                DocumentCount = docCount,
                UserCount = userCount,
                AuditLogCount = logCount
            };

            ViewBag.AuditLogStats = stats;
            return View(dashboard);
        }

        private async Task<int> GetCountAsync(HttpClient client, string endpoint)
        {
            var response = await client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return 0;

            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<int>(body);
        }
    }
}