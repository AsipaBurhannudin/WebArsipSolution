using System.Security.Claims;

namespace WebArsip.Mvc.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user?.IsInRole("Admin") ?? false;
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.Email)?.Value ?? "";
        }
    }
}