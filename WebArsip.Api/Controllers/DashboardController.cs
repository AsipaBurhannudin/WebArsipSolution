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

            // Admin: full counts
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var resultAdmin = new
                {
                    Documents = await _context.Documents.CountAsync(),
                    Users = await _context.Users.CountAsync(),
                    Roles = await _context.Roles.CountAsync(),
                    Permissions = await _context.Permissions.CountAsync(),
                    AuditLogs = await _context.AuditLogs.CountAsync(),
                    UserPermissions = await _context.UserPermissions.CountAsync()
                };
                return Ok(resultAdmin);
            }

            // Non-admin: documents = created by user OR docs that user has UserPermission.CanView
            var userDocsCount = await _context.Documents
                .Where(d => d.CreatedBy.ToLower() == userEmail.ToLower())
                .CountAsync();

            var permissionDocsCount = await _context.UserPermissions
                .Where(up => up.UserEmail.ToLower() == userEmail.ToLower() && up.CanView)
                .Select(up => up.DocId)
                .Distinct()
                .CountAsync();

            var totalDocs = userDocsCount + permissionDocsCount;

            // AuditLogs: try both UserId==email OR UserId==user.Id.ToString() to be compatible with older logs
            var auditLogCount = await _context.AuditLogs
                .Where(l => l.UserId == userEmail || l.UserId == user.Id.ToString() || l.UserId == userEmail.ToLower())
                .CountAsync();

            var result = new
            {
                Documents = totalDocs,
                Users = await _context.Users.CountAsync(),
                Roles = 0,
                Permissions = 0,
                AuditLogs = auditLogCount,
                UserPermissions = 0
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

                // match either email or id string
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                var idStr = user != null ? user.Id.ToString() : null;

                query = query.Where(l => l.UserId == userEmail || (idStr != null && l.UserId == idStr));
            }
            else if (!string.IsNullOrEmpty(email))
            {
                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (targetUser != null)
                    query = query.Where(l => l.UserId == targetUser.Id.ToString() || l.UserId == email);
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