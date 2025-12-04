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

        // 🔹 OVERRIDING RULE
        // 1. Admin = Full access
        // 2. UserPermission overrides RolePermission
        // 3. Owner = Full access
        public async Task<bool> HasDocumentAccessAsync(string email, IEnumerable<string> roles, Document doc, Func<Permission, bool> predicate)
        {
            // Admin full access
            if (roles.Contains("Admin"))
                return true;

            // Owner full access
            if (doc.CreatedBy.Equals(email, StringComparison.OrdinalIgnoreCase))
                return true;

            // USER PERMISSION (OVERRIDE)
            var userPerm = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.DocId == doc.DocId && up.UserEmail == email);

            if (userPerm != null)
            {
                var converted = new Permission
                {
                    CanView = userPerm.CanView,
                    CanEdit = userPerm.CanEdit,
                    CanDelete = userPerm.CanDelete,
                    CanUpload = userPerm.CanUpload,
                    CanDownload = userPerm.CanDownload
                };

                // UserPermission override → return langsung
                return predicate(converted);
            }

            // ROLE PERMISSION
            var rolePerms = await _context.Permissions
                .Include(p => p.Role)
                .Where(p => roles.Contains(p.Role.Name))
                .ToListAsync();

            return rolePerms.Any(p => predicate(p));
        }

        // dipakai ketika doc belum diketahui
        public async Task<bool> HasAccessAsync(string email, IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate)
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocId == docId);
            if (doc == null) return false;

            return await HasDocumentAccessAsync(email, roleNames, doc, predicate);
        }
    }
}