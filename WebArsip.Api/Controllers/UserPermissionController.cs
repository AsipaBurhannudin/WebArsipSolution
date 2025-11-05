using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;
using WebArsip.Infrastructure.Services;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // hanya admin yang bisa manage
    public class UserPermissionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditLogService _auditLogService;

        public UserPermissionController(AppDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // 🔹 Ambil semua user permission
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPermissionReadDto>>> GetAll()
        {
            var data = await _context.UserPermissions
                .Include(up => up.Document)
                .OrderBy(up => up.Id)
                .ToListAsync();

            return Ok(data.Select(up => new UserPermissionReadDto
            {
                Id = up.Id,
                DocId = up.DocId,
                DocTitle = up.Document?.Title ?? "(Dokumen tidak ditemukan)",
                UserEmail = up.UserEmail,
                CanView = up.CanView,
                CanEdit = up.CanEdit,
                CanDelete = up.CanDelete,
                CanDownload = up.CanDownload,
                CanUpload = up.CanUpload
            }));
        }

        // 🔹 Tambah user permission
        [HttpPost]
        public async Task<ActionResult<UserPermissionReadDto>> Create(UserPermissionCreateDto dto)
        {
            // Cek duplikat
            var exists = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.DocId == dto.DocId && up.UserEmail == dto.UserEmail);

            if (exists != null)
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "CREATE", "UserPermission", dto.DocId.ToString(),
                    $"UserPermission duplikat untuk user {dto.UserEmail}");
                return Conflict("Permission untuk user dan dokumen ini sudah ada.");
            }

            var entity = new UserPermission
            {
                DocId = dto.DocId,
                UserEmail = dto.UserEmail,
                CanView = dto.CanView,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete,
                CanDownload = dto.CanDownload,
                CanUpload = dto.CanUpload
            };

            _context.UserPermissions.Add(entity);
            await _context.SaveChangesAsync();

            var doc = await _context.Documents.FindAsync(dto.DocId);
            await _auditLogService.LogAsync(User, "CREATE", "UserPermission", entity.Id.ToString(),
                $"Admin memberi akses ke {dto.UserEmail} untuk dokumen '{doc?.Title ?? "Unknown"}' (DocId={dto.DocId})");

            return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, new UserPermissionReadDto
            {
                Id = entity.Id,
                DocId = entity.DocId,
                DocTitle = doc?.Title,
                UserEmail = entity.UserEmail,
                CanView = entity.CanView,
                CanEdit = entity.CanEdit,
                CanDelete = entity.CanDelete,
                CanDownload = entity.CanDownload,
                CanUpload = entity.CanUpload
            });
        }

        // 🔹 Update user permission
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UserPermissionCreateDto dto)
        {
            var entity = await _context.UserPermissions.FindAsync(id);
            if (entity == null)
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "UPDATE", "UserPermission", id.ToString(), "Data tidak ditemukan");
                return NotFound();
            }

            entity.CanView = dto.CanView;
            entity.CanEdit = dto.CanEdit;
            entity.CanDelete = dto.CanDelete;
            entity.CanDownload = dto.CanDownload;
            entity.CanUpload = dto.CanUpload;

            await _context.SaveChangesAsync();

            var doc = await _context.Documents.FindAsync(entity.DocId);
            await _auditLogService.LogAsync(User, "UPDATE", "UserPermission", id.ToString(),
                $"Admin memperbarui permission untuk {entity.UserEmail} pada dokumen '{doc?.Title ?? "Unknown"}' (DocId={entity.DocId})");

            return NoContent();
        }

        // 🔹 Hapus user permission
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.UserPermissions.FindAsync(id);
            if (entity == null)
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "DELETE", "UserPermission", id.ToString(), "Data tidak ditemukan");
                return NotFound();
            }

            _context.UserPermissions.Remove(entity);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(User, "DELETE", "UserPermission", id.ToString(),
                $"Admin menghapus permission untuk user {entity.UserEmail} (DocId={entity.DocId})");

            return NoContent();
        }
    }
}