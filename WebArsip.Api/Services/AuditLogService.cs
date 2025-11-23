using System.Security.Claims;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Infrastructure.Services
{
    public class AuditLogService
    {
        private readonly AppDbContext _context;
        private readonly TimeZoneInfo _wibZone;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
            _wibZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }

        // -----------------------------
        // 🔥 INTERNAL HELPER
        // -----------------------------
        private async Task WriteLogAsync(
            string userEmail,
            string action,
            string entityName,
            string entityId,
            string details)
        {
            var wibTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _wibZone);

            var log = new AuditLog
            {
                UserId = userEmail,
                Action = action,
                EntityName = entityName,
                EntityId = entityId ?? "-",
                Timestamp = wibTime,
                Details = details ?? "-"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // -----------------------------
        // 📌 Log CRUD Normal
        // -----------------------------
        public async Task LogAsync(
            ClaimsPrincipal user,
            string action,
            string entityName,
            string entityId,
            string details)
        {
            var userEmail =
                user.FindFirstValue(ClaimTypes.Email)
                ?? user.Identity?.Name
                ?? "unknown@system.local";

            await WriteLogAsync(userEmail, action, entityName, entityId, details);
        }

        // -----------------------------
        // 🚫 Log Permission Denied
        // -----------------------------
        public async Task LogPermissionDeniedAsync(
            ClaimsPrincipal user,
            string action,
            string entityName,
            string entityId,
            string reason)
        {
            var userEmail =
                user.FindFirstValue(ClaimTypes.Email)
                ?? user.Identity?.Name
                ?? "unknown@system.local";

            var msg = $"Access denied: {reason}";

            await WriteLogAsync(
                userEmail,
                $"DENIED_{action.ToUpper()}",
                entityName,
                entityId,
                msg
            );
        }

        // -----------------------------
        // 🔗 Log When Sharing Permission
        // -----------------------------
        public async Task LogSharePermissionAsync(
            ClaimsPrincipal admin,
            string targetUserEmail,
            string documentId,
            string details)
        {
            var adminEmail =
                admin.FindFirstValue(ClaimTypes.Email)
                ?? admin.Identity?.Name
                ?? "unknown@system.local";

            var msg = $"Admin {adminEmail} granted access to {targetUserEmail}. {details}";

            await WriteLogAsync(
                adminEmail,
                "SHARE_PERMISSION",
                "Document",
                documentId,
                msg
            );
        }
    }
}