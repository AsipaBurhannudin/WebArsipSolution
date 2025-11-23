using WebArsip.Core.Entities;

namespace WebArsip.Infrastructure.Services
{
    public interface IPermissionService
    {
        Task<bool> HasAccessAsync(string email, IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate);
        Task<bool> HasDocumentAccessAsync(string email, IEnumerable<string> roles, Document doc, Func<Permission, bool> predicate);
    }
}