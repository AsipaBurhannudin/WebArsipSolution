using Microsoft.AspNetCore.Http;
using System.Net.Http;
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
            var role = context.Session.GetString("UserRole");

            // Kalau belum login
            if (string.IsNullOrEmpty(role))
            {
                if (!path.Contains("/auth/login"))
                {
                    context.Response.Redirect("/Auth/Login");
                    return;
                }
            }

            // 🔒 Halaman khusus Admin
            var adminPages = new[] { "/user", "/role", "/permission" };
            if (adminPages.Any(p => path.StartsWith(p)) && role != "Admin")
            {
                context.Response.Redirect("/Error/AccessDenied");
                return;
            }

            await _next(context);
        }
    }
}