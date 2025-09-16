using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PermissionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionReadDto>>> GetPermissions()
        {
            var permissions = await _context.Permissions
                .Include(p => p.Role)
                .Include(p => p.Document)
                .ToListAsync();

            return permissions.Select(p => new PermissionReadDto
            {
                PermissionId = p.PermissionId,
                RoleId = p.RoleId,
                RoleName = p.Role.Name,
                DocId = p.DocId,
                DocTitle = p.Document.Title,
                CanView = p.CanView,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete,
                CanDownload = p.CanDownload,
                CanUpload = p.CanUpload
            }).ToList();
        }

        [HttpPost]
        public async Task<ActionResult<PermissionReadDto>> CreatePermission(PermissionCreateDto dto)
        {
            var permission = new Permission
            {
                RoleId = dto.RoleId,
                DocId = dto.DocId,
                CanView = dto.CanView,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete,
                CanDownload = dto.CanDownload,
                CanUpload = dto.CanUpload
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            var result = await _context.Permissions
                .Include(p => p.Role)
                .Include(p => p.Document)
                .FirstAsync(p => p.PermissionId == permission.PermissionId);

            return new PermissionReadDto
            {
                PermissionId = result.PermissionId,
                RoleId = result.RoleId,
                RoleName = result.Role.Name,
                DocId = result.DocId,
                DocTitle = result.Document.Title,
                CanView = result.CanView,
                CanEdit = result.CanEdit,
                CanDelete = result.CanDelete,
                CanDownload = result.CanDownload,
                CanUpload = result.CanUpload
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, PermissionCreateDto dto)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();

            permission.RoleId = dto.RoleId;
            permission.DocId = dto.DocId;
            permission.CanView = dto.CanView;
            permission.CanEdit = dto.CanEdit;
            permission.CanDelete = dto.CanDelete;
            permission.CanDownload = dto.CanDownload;
            permission.CanUpload = dto.CanUpload;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
