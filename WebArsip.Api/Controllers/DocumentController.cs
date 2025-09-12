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

        // 🔹 Helper: cek apakah user punya role Admin
        private bool IsAdmin(IEnumerable<string> roles) =>
            roles.Contains("Admin");

        // 🔹 Helper: cek permission berdasarkan role
        private async Task<bool> HasPermission(IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate)
        {
            // Admin selalu lolos
            if (IsAdmin(roleNames)) return true;

            var roleIds = await _context.Roles
                .Where(r => roleNames.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            if (!roleIds.Any()) return false;

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => roleIds.Contains(p.RoleId) && p.DocId == docId);

            return permission != null && predicate(permission);
        }
        // 🔹 GET /api/document
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentReadDto>>> GetAllDocuments()
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            // Kalau Admin → return semua dokumen
            if (roles.Contains("Admin"))
            {
                var allDocs = await _context.Documents.ToListAsync();
                return Ok(allDocs.Select(d => new DocumentReadDto
                {
                    DocId = d.DocId,
                    Title = d.Title,
                    Description = d.Description,
                    FilePath = d.FilePath,
                    CreatedDate = d.CreatedDate,
                    Status = d.Status
                }));
            }

            // Kalau bukan Admin → filter by permission
            var roleIds = await _context.Roles
                .Where(r => roles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var docs = await _context.Permissions
                .Where(p => roleIds.Contains(p.RoleId) && p.CanView)
                .Select(p => p.Document)
                .ToListAsync();

            return Ok(docs.Select(d => new DocumentReadDto
            {
                DocId = d.DocId,
                Title = d.Title,
                Description = d.Description,
                FilePath = d.FilePath,
                CreatedDate = d.CreatedDate,
                Status = d.Status
            }));
        }

        // 🔹 GET /api/document/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentReadDto>> GetDocument(int id)
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var canView = await HasPermission(roles, id, p => p.CanView);
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
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var canUpload = await HasPermission(roles, 1, p => p.CanUpload); // Admin auto true
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
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var canEdit = await HasPermission(roles, id, p => p.CanEdit);
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
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var canDelete = await HasPermission(roles, id, p => p.CanDelete);
            if (!canDelete) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 Archive endpoint
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchiveDocument(int id)
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var canArchive = await HasPermission(roles, id, p => p.CanDelete);
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