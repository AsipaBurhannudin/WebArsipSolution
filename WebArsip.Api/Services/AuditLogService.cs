using System.Security.Claims;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Infrastructure.Services
{
    public class AuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(ClaimsPrincipal user, string action, string entityName, string entityId, string details)
        {
            // Ambil userId dari token (claim nameid)
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fallback kalau tidak ada → pakai email/username
            if (string.IsNullOrEmpty(userId))
            {
                userId = user.Identity?.Name ?? "Unknown";
            }

            // Waktu Indonesia (WIB = GMT+7)
            var wibNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            );

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = wibNow,
                Details = details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}