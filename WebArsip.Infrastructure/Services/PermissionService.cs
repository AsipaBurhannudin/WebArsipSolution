using Microsoft.EntityFrameworkCore;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Role-based + User-based Permission Check
        public async Task<bool> HasAccessAsync(string email, IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate)
        {
            // Admin full access
            if (roleNames.Contains("Admin"))
                return true;

            // 1️⃣ Role-based Permission
            var rolePermissions = await _context.Permissions
                .Include(p => p.Role)
                .Where(p => roleNames.Contains(p.Role.Name))
                .ToListAsync();

            if (rolePermissions.Any(p => predicate(p)))
                return true;

            // 2️⃣ User-based Permission
            if (docId > 0)
            {
                var userPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserEmail == email && up.DocId == docId);

                if (userPermission != null)
                {
                    var converted = new Permission
                    {
                        CanView = userPermission.CanView,
                        CanEdit = userPermission.CanEdit,
                        CanDelete = userPermission.CanDelete,
                        CanUpload = userPermission.CanUpload,
                        CanDownload = userPermission.CanDownload
                    };

                    if (predicate(converted))
                        return true;
                }
            }

            return false;
        }

        // 🔹 Ambil Permission berdasarkan Role
        public async Task<Permission?> GetRolePermissionAsync(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return null;

            return await _context.Permissions.FirstOrDefaultAsync(p => p.RoleId == role.Id);
        }

        // 🔹 Generalized Feature Check
        public async Task<bool> HasPermissionAsync(string roleName, string feature)
        {
            if (string.IsNullOrEmpty(roleName)) return false;

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return false;

            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.RoleId == role.Id);
            if (permission == null) return false;

            return feature switch
            {
                Features.DocumentView => permission.CanView,
                Features.DocumentEdit => permission.CanEdit,
                Features.DocumentDelete => permission.CanDelete,
                Features.DocumentCreate => permission.CanUpload,
                Features.DocumentDownload => permission.CanDownload,
                _ => false
            };
        }

        // 🔹 Shortcut helpers
        public async Task<bool> CanViewAsync(string roleName) => await HasPermissionAsync(roleName, Features.DocumentView);
        public async Task<bool> CanEditAsync(string roleName) => await HasPermissionAsync(roleName, Features.DocumentEdit);
        public async Task<bool> CanDeleteAsync(string roleName) => await HasPermissionAsync(roleName, Features.DocumentDelete);
        public async Task<bool> CanUploadAsync(string roleName) => await HasPermissionAsync(roleName, Features.DocumentCreate);
        public async Task<bool> CanDownloadAsync(string roleName) => await HasPermissionAsync(roleName, Features.DocumentDownload);
    }
}