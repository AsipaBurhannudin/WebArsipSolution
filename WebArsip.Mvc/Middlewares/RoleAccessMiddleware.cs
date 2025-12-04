using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebArsip.Mvc.Middlewares
{
    public class RoleAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // ✅ Skip public/unauthenticated pages
            if (string.IsNullOrEmpty(path) ||
                path == "/" ||
                path.StartsWith("/auth") ||
                path.StartsWith("/error") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/images"))
            {
                await _next(context);
                return;
            }

            // ✅ Pastikan user sudah login
            var user = context.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Response.Redirect("/Auth/Login");
                return;
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? "";

            // 🔒 Batasi akses non-admin
            if ((path.Contains("/user") ||
                 path.Contains("/role") ||
                 path.Contains("/permission") ||
                 path.Contains("/auditlog")) &&
                !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Error/AccessDenied");
                return;
            }

            await _next(context);
        }
    }
}