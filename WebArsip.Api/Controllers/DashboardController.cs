using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Endpoint untuk semua count
        [HttpGet("counts")]
        public async Task<IActionResult> GetCounts()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(userEmail))
                return Forbid();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
                return NotFound("User tidak ditemukan");

            // 🔹 Query dasar sesuai role
            var documentQuery = _context.Documents.AsQueryable();
            var auditLogQuery = _context.AuditLogs.AsQueryable();

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Jika bukan admin → hanya lihat miliknya
                documentQuery = documentQuery.Where(d => d.CreatedBy == user.Id.ToString());
                auditLogQuery = auditLogQuery.Where(a => a.UserId == user.Id.ToString());
            }

            var result = new
            {
                Documents = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               ? await _context.Documents.CountAsync()
               : await _context.Documents.Where(d => d.CreatedBy == userEmail).CountAsync(),

                    Users = await _context.Users.CountAsync(),
                    Roles = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               ? await _context.Roles.CountAsync()
               : 0,
                    Permissions = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               ? await _context.Permissions.CountAsync()
               : 0,
                    AuditLogs = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               ? await _context.AuditLogs.CountAsync()
               : await _context.AuditLogs.Where(l => l.UserId == userEmail || l.UserId == userEmail.ToLower()).CountAsync(),
                    UserPermissions = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               ? await _context.UserPermissions.CountAsync()
               : 0
                };

            return Ok(result);
        }

        // ✅ Endpoint untuk grafik user activity
        [HttpGet("user-activity")]
        public async Task<IActionResult> GetUserActivity([FromQuery] string? email, [FromQuery] int days = 7)
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";
            var startDateUtc = DateTime.UtcNow.AddDays(-days);
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            var query = _context.AuditLogs.AsQueryable().Where(l => l.Timestamp >= startDateUtc);

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Forbid();

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                    return NotFound("User tidak ditemukan");

                query = query.Where(l => l.UserId == user.Id.ToString());
            }
            else if (!string.IsNullOrEmpty(email))
            {
                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (targetUser != null)
                    query = query.Where(l => l.UserId == targetUser.Id.ToString());
            }

            var logs = await query
                .Select(l => new
                {
                    Date = TimeZoneInfo.ConvertTimeFromUtc(l.Timestamp, tz).Date,
                    l.Action
                })
                .ToListAsync();

            var grouped = logs
                .GroupBy(l => new { l.Date, l.Action })
                .Select(g => new ActivityGroup
                {
                    Date = g.Key.Date.ToString("yyyy-MM-dd"),
                    Action = g.Key.Action,
                    Count = g.Count()
                })
                .ToList();

            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            var startDate = today.AddDays(-days);
            var dateRange = Enumerable.Range(0, days + 1)
                .Select(offset => startDate.AddDays(offset).ToString("yyyy-MM-dd"))
                .ToList();

            var allActions = grouped.Select(g => g.Action).Distinct().ToList();
            var filledData = new List<ActivityGroup>();

            foreach (var date in dateRange)
            {
                foreach (var action in allActions)
                {
                    var existing = grouped.FirstOrDefault(g => g.Date == date && g.Action == action);
                    filledData.Add(new ActivityGroup
                    {
                        Date = date,
                        Action = action,
                        Count = existing?.Count ?? 0
                    });
                }
            }

            return Ok(filledData.OrderBy(f => f.Date).ToList());
        }

        private class ActivityGroup
        {
            public string Date { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}