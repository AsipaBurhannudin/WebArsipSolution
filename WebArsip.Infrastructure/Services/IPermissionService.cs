using WebArsip.Core.Entities;

namespace WebArsip.Infrastructure.Services
{
    public interface IPermissionService
    {
        Task<bool> HasAccessAsync(string email, IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate);
        Task<Permission?> GetRolePermissionAsync(string roleName);
        Task<bool> HasPermissionAsync(string roleName, string feature);
        Task<bool> CanViewAsync(string roleName);
        Task<bool> CanEditAsync(string roleName);
        Task<bool> CanDeleteAsync(string roleName);
        Task<bool> CanUploadAsync(string roleName);
        Task<bool> CanDownloadAsync(string roleName);
    }
}