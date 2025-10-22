using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebArsip.Mvc.Helpers;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        private HttpClient CreateClient()
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        public DashboardController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index()
        {

            if (!UserRoleHelper.IsLoggedIn(HttpContext))
                return RedirectToAction("Login", "Auth");

            var client = CreateClient();

            var docCount = await GetCountAsync(client, "document/count");
            var userCount = await GetCountAsync(client, "user/count");
            var logCount = await GetCountAsync(client, "auditlog/count");

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

            if (docCount == 0 && userCount == 0 && logCount == 0)
                TempData["Warning"] = "Gagal memuat statistik dashboard. Silakan refresh.";

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