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
        private IEnumerable<string> GetUserRoles() =>
            User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        private static DateTime NowLocal() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // ============================
        // Universal Access Checker
        // - docId == 0 -> global check (role-based)
        // - docId > 0  -> check role-based OR per-user permission for that doc
        // Email comparison case-insensitive
        // ============================
        private async Task<bool> HasAccessAsync(string email, IEnumerable<string> roleNames, int docId, Func<UserPermission, bool> checkPermission)
        {
            // 1. Global permission: hanya berlaku untuk aksi CREATE
            if (docId == 0)
            {
                var rolePerms = await _context.Permissions
                    .Include(p => p.Role)
                    .Where(p => roleNames.Contains(p.Role.Name))
                    .ToListAsync();

                return checkPermission(new UserPermission
                {
                    CanView = rolePerms.Any(p => p.CanView),
                    CanEdit = rolePerms.Any(p => p.CanEdit),
                    CanDelete = rolePerms.Any(p => p.CanDelete),
                    CanUpload = rolePerms.Any(p => p.CanUpload),
                    CanDownload = rolePerms.Any(p => p.CanDownload)
                });
            }

            // 2. Ambil dokumen
            var doc = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DocId == docId);

            if (doc == null)
                return false;

            bool isOwner = doc.CreatedBy.Equals(email, StringComparison.OrdinalIgnoreCase);
            bool isAdmin = roleNames.Contains("Admin");

            // Owner dan Admin boleh semua
            if (isOwner || isAdmin)
                return true;

            // 3. Hanya cek UserPermission UTK dokumen orang lain
            var up = await _context.UserPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DocId == docId && x.UserEmail.ToLower() == email.ToLower());

            if (up != null && checkPermission(up))
                return true;

            // 4. Tidak boleh fallback ke RolePermission — ini yang sebelumnya bikin jebol
            return false;
        }

        // ============================
        // GET All Documents (visibility improved)
        // Non-admin sees:
        //  - documents they created (CreatedBy equals email)
        //  - documents that have UserPermission for them with CanView = true
        // ============================
        [HttpGet]
        public async Task<ActionResult<PagedResult<DocumentReadDto>>> GetAllDocuments([FromQuery] BaseQueryDto query)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

            IQueryable<Document> docsQuery = _context.Documents.AsQueryable();

            // Pastikan user memiliki hak view global (role-level) OR at least some per-doc permission will be used later.
            // If user has neither global CanView nor any per-doc CanView, block.
            var globalCanView = await HasAccessAsync(email, roles, 0, p => p.CanView);
            var anyPerDocView = await _context.UserPermissions.AnyAsync(up => up.UserEmail.ToLower() == email.ToLower() && up.CanView);

            if (!globalCanView && !anyPerDocView)
            {
                // no view access at all
                await _auditLogService.LogPermissionDeniedAsync(User, "LIST", "Document", "N/A", "User lacks any view permission");
                return Forbid();
            }

            // Non-admin: restrict to createdBy OR per-doc permitted docs
            if (!roles.Contains("Admin"))
            {
                var allowedIds = await _context.UserPermissions
                    .Where(up => up.UserEmail.ToLower() == email.ToLower() && up.CanView)
                    .Select(up => up.DocId)
                    .ToListAsync();

                docsQuery = docsQuery.Where(d =>
                    d.CreatedBy.ToLower() == email.ToLower() || allowedIds.Contains(d.DocId));
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
                    CreatedBy = d.CreatedBy,
                    CreatedDate = d.CreatedDate,
                    UpdatedAt = d.UpdatedAt,
                    Status = d.Status,
                    Version = d.Version
                }).ToList()
            });
        }

        // ============================
        // GET By Id
        // ============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

            if (!await HasAccessAsync(email, roles, id, p => p.CanView))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "VIEW", "Document", id.ToString(), "User lacks view permission");
                return Forbid();
            }

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocId == id);
            if (doc == null) return NotFound();

            return Ok(new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                OriginalFileName = doc.OriginalFileName,
                CreatedBy = doc.CreatedBy,
                CreatedDate = doc.CreatedDate,
                Status = doc.Status,
                Version = doc.Version
            });
        }

        // ============================
        // CREATE
        // ensure CreatedBy uses email (consistent)
        // ============================
        [HttpPost]
        public async Task<IActionResult> CreateDocument(DocumentCreateDto dto)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

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
                CreatedBy = email // store email consistently
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(User, "CREATE", "Document", doc.DocId.ToString(), $"Dokumen dibuat: {doc.Title}");

            return Ok(new { success = true, message = "Dokumen berhasil dibuat.", id = doc.DocId });
        }

        // ============================
        // UPDATE
        // ============================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, DocumentCreateDto dto)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocId == id);
            if (doc == null) return NotFound();

            bool isOwner = doc.CreatedBy.Equals(email, StringComparison.OrdinalIgnoreCase);
            bool isAdmin = roles.Contains("Admin");

            // STEP 1: Owner & Admin boleh edit
            if (!isOwner && !isAdmin)
            {
                // STEP 2: Shared permission check
                var up = await _context.UserPermissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DocId == id && x.UserEmail.ToLower() == email.ToLower());

                if (up == null || !up.CanEdit)
                {
                    await _auditLogService.LogPermissionDeniedAsync(User, "UPDATE", "Document", id.ToString(),
                        "User lacks edit permission");
                    return Forbid();
                }
            }

            bool changed = false;

            if (doc.Title != dto.Title) { doc.Title = dto.Title; changed = true; }
            if (doc.Description != dto.Description) { doc.Description = dto.Description; changed = true; }

            if (!string.IsNullOrWhiteSpace(dto.FilePath) && dto.FilePath != doc.FilePath)
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

                await _auditLogService.LogAsync(User, "UPDATE", "Document", id.ToString(),
                    $"Dokumen diperbarui oleh {email}");
            }

            return Ok(new { success = true, message = "Dokumen berhasil diperbarui." });
        }

        // ============================
        // DELETE
        // - Extra safety checks:
        //   Only allow if HasAccessAsync(... CanDelete) true.
        //   Log permission denied otherwise.
        // ============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocId == id);
            if (doc == null)
                return NotFound();

            bool isOwner = doc.CreatedBy.Equals(email, StringComparison.OrdinalIgnoreCase);
            bool isAdmin = roles.Contains("Admin");

            // STEP 1 — Owner always allowed
            if (isOwner || isAdmin)
            {
                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync(User, "DELETE", "Document", id.ToString(), $"Dokumen dihapus: {doc.Title}");
                return Ok(new { success = true, message = "Dokumen berhasil dihapus!" });
            }

            // STEP 2 — Shared document permission
            var up = await _context.UserPermissions
                .FirstOrDefaultAsync(x => x.DocId == id && x.UserEmail.ToLower() == email.ToLower());

            if (up == null || !up.CanDelete)
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "DELETE", "Document", id.ToString(),
                    "User lacks delete permission");
                return Forbid();
            }

            // STEP 3 — Only now delete (shared delete)
            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(User, "DELETE", "Document", id.ToString(),
                $"Dokumen dihapus oleh shared user: {email}");

            return Ok(new { success = true, message = "Dokumen berhasil dihapus!" });
        }

        // ============================
        // DOWNLOAD
        // ============================
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

            if (!await HasAccessAsync(email, roles, id, p => p.CanDownload))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "DOWNLOAD", "Document", id.ToString(), "User lacks download permission");
                return Forbid();
            }

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocId == id);
            if (doc == null) return NotFound();

            var path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "WebArsipStorage", "uploads", doc.FilePath);
            if (!System.IO.File.Exists(path))
                return NotFound("File tidak ditemukan di server.");

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(bytes, "application/octet-stream", doc.OriginalFileName ?? Path.GetFileName(path));
        }

        // ============================
        // PREVIEW
        // ============================
        [HttpGet("preview/{id}")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var email = GetUserEmail();
            var roles = GetUserRoles();

            if (!await HasAccessAsync(email, roles, id, p => p.CanView))
            {
                await _auditLogService.LogPermissionDeniedAsync(User, "VIEW", "Document", id.ToString(), "User lacks view permission");
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