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
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditLogService _auditLogService;

        public DocumentController(AppDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        private bool IsAdmin(IEnumerable<string> roles) => roles.Contains("Admin");

        private async Task<bool> HasPermission(IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate)
        {
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

        private string GetStorageFilePath(string relativeFilePath)
        {
            var root = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage");
            var relative = relativeFilePath.TrimStart('/', '\\');
            return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<DocumentReadDto>>> GetAllDocuments([FromQuery] BaseQueryDto query)
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            IQueryable<Document> docsQuery = _context.Documents;

            if (!roles.Contains("Admin"))
            {
                var roleIds = await _context.Roles
                    .Where(r => roles.Contains(r.Name))
                    .Select(r => r.Id)
                    .ToListAsync();

                docsQuery = _context.Permissions
                    .Where(p => roleIds.Contains(p.RoleId) && p.CanView)
                    .Select(p => p.Document);
            }

            var totalCount = await docsQuery.CountAsync();

            var docs = await docsQuery
                .OrderByDescending(d => d.CreatedDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var result = new PagedResult<DocumentReadDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                Items = docs.Select(d => new DocumentReadDto
                {
                    DocId = d.DocId,
                    Title = d.Title,
                    Description = d.Description,
                    FilePath = d.FilePath,
                    OriginalFileName = d.OriginalFileName,
                    CreatedDate = d.CreatedDate,
                    UpdatedAt = d.UpdatedAt,
                    Status = d.Status
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentReadDto>> GetDocument(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            return new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                OriginalFileName = doc.OriginalFileName,
                CreatedDate = doc.CreatedDate,
                UpdatedAt = doc.UpdatedAt,
                Status = doc.Status
            };
        }

        [HttpPost]
        public async Task<ActionResult<DocumentReadDto>> CreateDocument(DocumentCreateDto dto)
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            if (!await HasPermission(roles, 1, p => p.CanUpload)) return Forbid();

            var wibNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            var doc = new Document
            {
                Title = dto.Title,
                Description = dto.Description,
                FilePath = dto.FilePath,
                OriginalFileName = dto.OriginalFileName,
                CreatedDate = wibNow,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Draft" : dto.Status
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(User, "CREATE", "Document", doc.DocId.ToString(), $"Dokumen baru dibuat: {doc.Title}");

            return CreatedAtAction(nameof(GetDocument), new { id = doc.DocId }, new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                OriginalFileName = doc.OriginalFileName,
                CreatedDate = doc.CreatedDate,
                Status = doc.Status
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, DocumentCreateDto dto)
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            if (!await HasPermission(roles, id, p => p.CanEdit)) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var wibNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            doc.Title = dto.Title;
            doc.Description = dto.Description;
            doc.FilePath = dto.FilePath;
            doc.OriginalFileName = dto.OriginalFileName;
            doc.UpdatedAt = wibNow;

            if (doc.Status == "Published")
                doc.Status = "Updated";
            else if (!string.IsNullOrEmpty(dto.Status))
                doc.Status = dto.Status;

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(User, "UPDATE", "Document", id.ToString(), $"Dokumen diperbarui: {doc.Title}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            if (!await HasPermission(roles, id, p => p.CanDelete)) return Forbid();

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(User, "DELETE", "Document", id.ToString(), $"Dokumen dihapus: {doc.Title}");
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("stream/{id}")]
        public async Task<IActionResult> StreamFile(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null || string.IsNullOrEmpty(doc.FilePath))
                return NotFound();

            var storagePath = Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                "WebArsipStorage", "uploads"
            );

            var filePath = Path.Combine(storagePath, Path.GetFileName(doc.FilePath ?? ""));
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server.");

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var mimeType = "application/octet-stream";

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            return File(stream, mimeType, doc.OriginalFileName ?? Path.GetFileName(filePath));
        }

        [AllowAnonymous]
        [HttpGet("preview/{id}")]
        public async Task<IActionResult> PreviewFile(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null || string.IsNullOrEmpty(doc.FilePath))
                return NotFound("Dokumen tidak ditemukan.");

            var storagePath = Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                "WebArsipStorage", "uploads"
            );
            var filePath = Path.Combine(storagePath, Path.GetFileName(doc.FilePath ?? ""));

            if (!System.IO.File.Exists(filePath))
                return NotFound("File tidak ditemukan di server.");

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            // ⚠️ Penting: gunakan inline agar tampil, bukan download
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{doc.OriginalFileName}\"";

            return new FileStreamResult(stream, mimeType);
        }
    }
}
