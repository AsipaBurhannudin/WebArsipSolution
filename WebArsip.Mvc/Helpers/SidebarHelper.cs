using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Mvc.Helpers
{
    public static class SidebarHelper
    {
        public static List<(string Controller, string Label, string Icon)> GetAccessibleMenus(HttpContext httpContext)
        {
            var role = httpContext.Session.GetString("UserRole") ?? "";
            var roleIdStr = httpContext.Session.GetString("RoleId");

            // Jika belum login, return kosong
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(roleIdStr))
                return new List<(string, string, string)>();

            // Admin dapat semua menu
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new List<(string, string, string)>
                {
                    ("Dashboard", "🏠 Dashboard", "Dashboard"),
                    ("Document", "📄 Documents", "Document"),
                    ("User", "👤 Users", "User"),
                    ("Role", "🔑 Roles", "Role"),
                    ("Permission", "⚙ Permissions", "Permission"),
                    ("UserPermission", "🗂️ Doc User Permission", "AuditLog"),
                      ("SerialNumber", "🖥️ Serial Number Manage", "SerialNumber"),
                    ("AuditLog", "📊 Audit Logs", "AuditLog")
                };
            }

            // Non-admin ambil permission dari DB
            using var scope = httpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!int.TryParse(roleIdStr, out int roleId))
                return new List<(string, string, string)>();

            var allowedMenus = new List<(string, string, string)>
            {
                ("Dashboard", "🏠 Dashboard", "Dashboard"),
                ("Document", "📄 Documents", "Document")
            };

            // Tambah menu tambahan berdasarkan permission table
            var perms = db.Permissions.Where(p => p.RoleId == roleId).ToList();

            if (perms.Any(p => p.CanView))
            {
                allowedMenus.Add(("Document", "📄 Documents", "Document"));
            }

            if (perms.Any(p => p.CanUpload || p.CanEdit))
            {
                allowedMenus.Add(("Document", "📄 Manage Documents", "Document"));
            }

            if (perms.Any(p => p.CanDownload))
            {
                allowedMenus.Add(("Document", "⬇ Download Center", "Document"));
            }

            // Bisa kamu kembangkan kalau permission table punya flag lain (misalnya CanAudit, CanPolicy, dll)
            return allowedMenus.Distinct().ToList();
        }
    }
}