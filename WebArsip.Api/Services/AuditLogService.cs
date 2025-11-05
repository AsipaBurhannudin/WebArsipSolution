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

        // ✅ Log biasa (CRUD)
        public async Task LogAsync(ClaimsPrincipal user, string action, string entityName, string entityId, string details)
        {
            var userEmail = user.FindFirstValue(ClaimTypes.Email)
                           ?? user.Identity?.Name
                           ?? "unknown@system.local";

            var wibNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            );

            var log = new AuditLog
            {
                UserId = userEmail, // <-- gunakan Email sebagai ID
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = wibNow,
                Details = details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogPermissionDeniedAsync(ClaimsPrincipal user, string action, string entityName, string entityId, string reason)
        {
            var userEmail = user.FindFirstValue(ClaimTypes.Email)
                           ?? user.Identity?.Name
                           ?? "unknown@system.local";

            var wibNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            );

            var log = new AuditLog
            {
                UserId = userEmail,
                Action = $"DENIED_{action.ToUpper()}",
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = wibNow,
                Details = $"Access denied for {userEmail}: {reason}"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}