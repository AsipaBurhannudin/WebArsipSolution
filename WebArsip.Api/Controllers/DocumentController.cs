using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // semua endpoint butuh login
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DocumentController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Helper: cek permission
        private async Task<bool> HasPermission(string roleName, int docId, Func<Permission, bool> predicate)
        {
            var roleId = await _context.Roles
                .Where(r => r.RoleName == roleName)
                .Select(r => r.RoleId)
                .FirstOrDefaultAsync();

            if (roleId == 0) return false;

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.RoleId == roleId && p.DocId == docId);

            return permission != null && predicate(permission);
        }

        // 🔹 GET /api/document/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentReadDto>> GetDocument(int id)
        {
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleName == null) return Unauthorized();

            var canView = await HasPermission(roleName, id, p => p.CanView);
            if (!canView) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            return new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                CreatedDate = doc.CreatedDate,
                Status = doc.Status
            };
        }

        // 🔹 POST /api/document
        [HttpPost]
        public async Task<ActionResult<DocumentReadDto>> CreateDocument(DocumentCreateDto dto)
        {
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleName == null) return Unauthorized();

            // karena dokumen baru → pakai doc dummy (DocId = 1) untuk cek izin upload
            var canUpload = await HasPermission(roleName, 1, p => p.CanUpload);
            if (!canUpload) return Forbid();

            var doc = new Document
            {
                Title = dto.Title,
                Description = dto.Description,
                FilePath = dto.FilePath,
                CreatedDate = DateTime.UtcNow,
                Status = dto.Status
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            return new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                CreatedDate = doc.CreatedDate,
                Status = doc.Status
            };
        }

        // 🔹 PUT /api/document/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, DocumentCreateDto dto)
        {
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleName == null) return Unauthorized();

            var canEdit = await HasPermission(roleName, id, p => p.CanEdit);
            if (!canEdit) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            doc.Title = dto.Title;
            doc.Description = dto.Description;
            doc.FilePath = dto.FilePath;
            doc.UpdatedAt = DateTime.UtcNow;
            doc.Status = dto.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 DELETE /api/document/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleName == null) return Unauthorized();

            var canDelete = await HasPermission(roleName, id, p => p.CanDelete);
            if (!canDelete) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 Archive endpoint khusus Compliance (pakai CanDelete = archive)
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchiveDocument(int id)
        {
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleName == null) return Unauthorized();

            var canArchive = await HasPermission(roleName, id, p => p.CanDelete);
            if (!canArchive) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var archive = new Archive
            {
                DocId = doc.DocId,
                ArchivedAt = DateTime.UtcNow
            };

            _context.Archives.Add(archive);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Document {doc.Title} archived successfully" });
        }
    }
}