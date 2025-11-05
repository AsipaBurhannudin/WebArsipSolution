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
        private readonly PermissionService _permissionService;

        public DocumentController(AppDbContext context, AuditLogService auditLogService, PermissionService permissionService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _permissionService = permissionService;
        }

        private string GetUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        private string? GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value;
        private bool IsAdmin(string? role) => role == "Admin";
        private static DateTime NowLocal() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // ✅ Check Access (Role + User-level)
        private async Task<bool> HasAccessAsync(string email, IEnumerable<string> roleNames, int docId, Func<Permission, bool> predicate)
        {
            if (roleNames.Contains("Admin"))
                return true;

            var rolePermissions = await _context.Permissions
                .Include(p => p.Role)
                .Where(p => p.Role != null && roleNames.Contains(p.Role.Name))
                .ToListAsync();

            if (rolePermissions.Any(predicate))
                return true;

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

            return false;
        }

        // ✅ GET All Documents
        [HttpGet]
        public async Task<ActionResult<PagedResult<DocumentReadDto>>> GetAllDocuments([FromQuery] BaseQueryDto query)
        {
            var email = GetUserEmail();
            var role = GetUserRole();

            IQueryable<Document> docsQuery = _context.Documents.AsQueryable();

            if (!IsAdmin(role))
            {
                // User non-admin hanya bisa lihat dokumen yang diizinkan
                var allowedIds = await _context.UserPermissions
                    .Where(up => up.UserEmail == email && up.CanView)
                    .Select(up => up.DocId)
                    .ToListAsync();

                docsQuery = docsQuery.Where(d => allowedIds.Contains(d.DocId));
            }

            var totalCount = await docsQuery.CountAsync();
            var docs = await docsQuery
                .OrderByDescending(d => d.CreatedDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return Ok(new PagedResult<DocumentReadDto>
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
                    Status = d.Status,
                    Version = d.Version
                }).ToList()
            });
        }

        // ✅ GET By Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            return Ok(new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                OriginalFileName = doc.OriginalFileName,
                CreatedDate = doc.CreatedDate,
                Status = doc.Status,
                Version = doc.Version
            });
        }

        // ✅ CREATE Document
        [HttpPost]
        public async Task<IActionResult> CreateDocument(DocumentCreateDto dto)
        {
            var email = GetUserEmail();
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var role = GetUserRole();

            // 🔹 Cek apakah user punya izin upload
            if (!await HasAccessAsync(email, roles, 0, p => p.CanUpload))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "CREATE", "Document", "N/A", "User lacks upload permission");
                return Forbid();
            }

            var now = NowLocal();

            var doc = new Document
            {
                Title = dto.Title,
                Description = dto.Description,
                FilePath = dto.FilePath,
                OriginalFileName = dto.OriginalFileName,
                CreatedDate = now,
                Status = dto.Status ?? "Draft",
                Version = 1,
                CreatedBy = email
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            // ✅ Auto Ownership Logic
            // Hanya berlaku untuk Compliance, Audit, Policy
            if (role is "Compliance" or "Audit" or "Policy")
            {
                var existingPerm = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserEmail == email && up.DocId == doc.DocId);

                if (existingPerm == null)
                {
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserEmail = email,
                        DocId = doc.DocId,
                        CanView = true,
                        CanEdit = true,
                        CanUpload = true,
                        CanDownload = true,
                        CanDelete = true
                    });
                    await _context.SaveChangesAsync();
                }
            }

            // 🔹 Log pembuatan dokumen
            await _auditLogService.LogAsync(User, "CREATE", "Document", doc.DocId.ToString(), $"Dokumen dibuat: {doc.Title}");

            return Ok(new { success = true, message = "Dokumen berhasil dibuat.", id = doc.DocId });
        }

        // ✅ UPDATE Document
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, DocumentCreateDto dto)
        {
            var email = GetUserEmail();
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            if (!await HasAccessAsync(email, roles, id, p => p.CanEdit))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "UPDATE", "Document", id.ToString(), "User lacks edit permission");
                return Forbid();
            }

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            bool changed = false;

            if (doc.Title != dto.Title) { doc.Title = dto.Title; changed = true; }
            if (doc.Description != dto.Description) { doc.Description = dto.Description; changed = true; }

            if (!string.IsNullOrEmpty(dto.FilePath) && dto.FilePath != doc.FilePath)
            {
                doc.FilePath = dto.FilePath;
                doc.OriginalFileName = dto.OriginalFileName;
                changed = true;
            }

            if (changed)
            {
                doc.Version += 1;
                doc.UpdatedAt = NowLocal();
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync(User, "UPDATE", "Document", id.ToString(), $"Dokumen diperbarui: {doc.Title}");
            }

            return Ok(new { success = true, message = "Dokumen berhasil diperbarui." });
        }

        // ✅ DELETE Document
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var email = GetUserEmail();
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            if (!await HasAccessAsync(email, roles, id, p => p.CanDelete))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "DELETE", "Document", id.ToString(), "User lacks delete permission");
                return Forbid();
            }

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(User, "DELETE", "Document", id.ToString(), $"Dokumen dihapus: {doc.Title}");

            return Ok(new { success = true, message = "Dokumen berhasil dihapus!" });
        }

        // ✅ DOWNLOAD
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var email = GetUserEmail();
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            if (!await HasAccessAsync(email, roles, id, p => p.CanDownload))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "DOWNLOAD", "Document", id.ToString(), "User lacks download permission");
                return Forbid();
            }

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads", doc.FilePath);
            if (!System.IO.File.Exists(path))
                return NotFound("File tidak ditemukan di server.");

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(bytes, "application/octet-stream", doc.OriginalFileName ?? Path.GetFileName(path));
        }

        // ✅ STREAM for Preview (MVC)
        [HttpGet("stream/{id}")]
        public async Task<IActionResult> StreamDocument(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null || string.IsNullOrEmpty(doc.FilePath))
                return NotFound();

            var path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads", doc.FilePath);
            if (!System.IO.File.Exists(path))
                return NotFound("File tidak ditemukan di server.");

            var ext = Path.GetExtension(path).ToLowerInvariant();
            var mime = ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return new FileStreamResult(stream, mime);
        }

        // ✅ PREVIEW Document
        [HttpGet("preview/{id}")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var email = GetUserEmail();
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            if (!await HasAccessAsync(email, roles, id, p => p.CanView))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "VIEW", "Document", id.ToString(), "User lacks view/preview permission");
                return Forbid();
            }

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads", doc.FilePath);
            if (!System.IO.File.Exists(path))
                return NotFound();

            var mime = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{doc.OriginalFileName}\"";
            return new FileStreamResult(stream, mime);
        }
    }
}