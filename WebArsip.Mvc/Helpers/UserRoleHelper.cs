using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Mvc.Helpers
{
    public static class UserRoleHelper
    {
       /* public static bool HasAccess(HttpContext context, string permissionKey)
        {
            var claims = context.User.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToList();

            return claims.Contains(permissionKey);
        }*/
        public static bool IsLoggedIn(HttpContext httpContext) =>
            !string.IsNullOrEmpty(httpContext.Session.GetString("UserEmail"));

        public static string? GetUserEmail(HttpContext httpContext) =>
            httpContext.Session.GetString("UserEmail");

        public static string[] GetUserRoles(HttpContext httpContext)
        {
            var roles = httpContext.Session.GetString("UserRole");
            return string.IsNullOrEmpty(roles) ? Array.Empty<string>() : roles.Split(',');
        }

        public static bool HasAccess(HttpContext httpContext, string feature, int? docId = null)
        {
            var roles = GetUserRoles(httpContext);

            if (roles.Contains("Admin"))
                return true;

            var roleIdString = httpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleIdString))
                return false;

            if (!int.TryParse(roleIdString, out int roleId))
                return false;

            using var scope = httpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = db.Permissions.Where(p => p.RoleId == roleId);

            if (docId.HasValue)
                query = query.Where(p => p.DocId == docId);

            return feature switch
            {
                "Document.View" => query.Any(p => p.CanView),
                "Document.Create" => query.Any(p => p.CanUpload),
                "Document.Edit" => query.Any(p => p.CanEdit),
                "Document.Delete" => query.Any(p => p.CanDelete),
                "Document.Download" => query.Any(p => p.CanDownload),
                _ => false
            };
        }
    }
}