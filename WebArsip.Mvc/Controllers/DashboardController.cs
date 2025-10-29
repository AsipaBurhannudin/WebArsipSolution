using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebArsip.Mvc.Helpers;
using WebArsip.Mvc.Models.ViewModels;

namespace WebArsip.Mvc.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public DashboardController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // ✅ Membuat HttpClient dengan JWT dari session
        private HttpClient CreateClient()
        {
            var client = _clientFactory.CreateClient("WebArsipApi");
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        // ✅ Endpoint utama
        public async Task<IActionResult> Index()
        {
            if (!UserRoleHelper.IsLoggedIn(HttpContext))
                return RedirectToAction("Login", "Auth");

            var role = HttpContext.Session.GetString("UserRole") ?? "User";

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return await LoadAdminDashboard();
            else
                return await LoadUserDashboard();
        }

        // 🔹 DASHBOARD ADMIN
        private async Task<IActionResult> LoadAdminDashboard()
        {
            var client = CreateClient();

            // Ambil berbagai count
            var dashboard = new DashboardViewModel
            {
                DocumentCount = await GetCountAsync(client, "document/count"),
                UserCount = await GetCountAsync(client, "user/count"),
                AuditLogCount = await GetCountAsync(client, "auditlog/count"),
                RoleCount = await GetCountAsync(client, "role/count"),
                PermissionCount = await GetCountAsync(client, "permission/count")
            };

            // Ambil data statistik auditlog (grafik aktivitas)
            var stats = new List<AuditLogStatsViewModel>();
            try
            {
                var statsResponse = await client.GetAsync("auditlog/daily-stats?days=7");
                if (statsResponse.IsSuccessStatusCode)
                {
                    var body = await statsResponse.Content.ReadAsStringAsync();
                    stats = JsonConvert.DeserializeObject<List<AuditLogStatsViewModel>>(body) ?? new();
                }
            }
            catch
            {
                // Abaikan jika API down
            }

            ViewBag.AuditLogStats = stats;
            return View("Index_Admin", dashboard);
        }

        // 🔹 DASHBOARD USER
        private async Task<IActionResult> LoadUserDashboard()
        {
            var client = CreateClient();
            var email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;

            // Ambil jumlah dokumen milik user ini
            int userDocs = await GetCountAsync(client, $"document/count-by-user?email={email}");

            // Karena AuditLogController di-Authorize Admin, user biasa gak bisa akses API log,
            // jadi biarkan AuditLogCount = 0 untuk sekarang
            var dashboard = new DashboardViewModel
            {
                DocumentCount = userDocs,
                UserCount = 0,
                AuditLogCount = 0
            };

            return View("Index_User", dashboard);
        }

        // 🔹 Helper umum untuk ambil integer count
        private async Task<int> GetCountAsync(HttpClient client, string endpoint)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode) return 0;

                var body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<int>(body);
            }
            catch
            {
                return 0;
            }
        }
    }
}