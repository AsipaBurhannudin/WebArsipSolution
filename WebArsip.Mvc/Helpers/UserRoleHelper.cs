using Microsoft.AspNetCore.Http;

namespace WebArsip.Mvc.Helpers
{
    public static class UserRoleHelper
    {
        public static bool IsLoggedIn(HttpContext httpContext) =>
            !string.IsNullOrEmpty(httpContext.Session.GetString("UserEmail"));

        public static string? GetUserEmail(HttpContext httpContext) =>
            httpContext.Session.GetString("UserEmail");

        public static string[] GetUserRoles(HttpContext httpContext)
        {
            var roles = httpContext.Session.GetString("UserRole");
            return string.IsNullOrEmpty(roles) ? new string[0] : roles.Split(',');
        }

        public static bool HasAccess(HttpContext httpContext, string feature)
        {
            var roles = GetUserRoles(httpContext);

            // Mapping feature ↔ role
            return feature switch
            {
                "Dashboard" => roles.Any(), // semua role bisa lihat dashboard
                "Documents" => roles.Any(r => r is "Admin" or "Policy" or "Compliance" or "Audit"),
                "UserManagement" => roles.Contains("Admin"),
                "RoleManagement" => roles.Contains("Admin"),
                "PermissionManagement" => roles.Contains("Admin"),
                "AuditLogs" => roles.Contains("Admin") || roles.Contains("Audit"),
                "PolicyManagement" => roles.Contains("Policy"),
                "ComplianceReports" => roles.Contains("Compliance"),
                _ => false
            };
        }
    }
}