using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;
using WebArsip.Infrastructure.Services;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserPermissionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditLogService _auditLogService;

        public UserPermissionController(AppDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // 📌 GET: api/UserPermission?page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserPermissionReadDto>>> GetAll([FromQuery] BaseQueryDto query)
        {
            var baseQuery = _context.UserPermissions
                .Include(up => up.User)
                .Include(up => up.Document)
                .AsQueryable();

            var totalCount = await baseQuery.CountAsync();

            var data = await baseQuery
                .OrderByDescending(up => up.UserPermissionId)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(up => new UserPermissionReadDto
                {
                    UserPermissionId = up.UserPermissionId,
                    UserId = up.UserId,
                    UserName = up.User.Name,
                    UserEmail = up.User.Email,
                    DocId = up.DocId,
                    DocumentTitle = up.Document.Title,
                    CanView = up.CanView,
                    CanUpload = up.CanUpload,
                    CanEdit = up.CanEdit,
                    CanDelete = up.CanDelete
                })
                .ToListAsync();

            return Ok(new PagedResult<UserPermissionReadDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                Items = data
            });
        }

        // 📌 GET: api/UserPermission/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserPermissionReadDto>> Get(int id)
        {
            var up = await _context.UserPermissions
                .Include(u => u.User)
                .Include(d => d.Document)
                .FirstOrDefaultAsync(x => x.UserPermissionId == id);

            if (up == null) return NotFound();

            return new UserPermissionReadDto
            {
                UserPermissionId = up.UserPermissionId,
                UserId = up.UserId,
                UserName = up.User?.Name ?? "-",
                UserEmail = up.User?.Email ?? "-",
                DocId = up.DocId,
                DocumentTitle = up.Document?.Title ?? "-",
                CanView = up.CanView,
                CanUpload = up.CanUpload,
                CanEdit = up.CanEdit,
                CanDelete = up.CanDelete
            };
        }

        // 📌 POST: api/UserPermission
        [HttpPost]
        public async Task<ActionResult<UserPermissionReadDto>> Create(UserPermissionCreateDto dto)
        {
            var up = new UserPermission
            {
                UserId = dto.UserId,
                DocId = dto.DocId,
                CanView = dto.CanView,
                CanUpload = dto.CanUpload,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete
            };

            _context.UserPermissions.Add(up);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(User, "CREATE", "UserPermission", up.UserPermissionId.ToString(),
                $"UserPermission ditambahkan untuk UserId={dto.UserId}, DocumentId={dto.DocId}");

            return CreatedAtAction(nameof(Get), new { id = up.UserPermissionId }, new UserPermissionReadDto
            {
                UserPermissionId = up.UserPermissionId,
                UserId = up.UserId,
                DocId = up.DocId,
                CanView = up.CanView,
                CanUpload = up.CanUpload,
                CanEdit = up.CanEdit,
                CanDelete = up.CanDelete
            });
        }

        // 📌 PUT: api/UserPermission/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UserPermissionCreateDto dto)
        {
            var up = await _context.UserPermissions.FindAsync(id);
            if (up == null) return NotFound();

            up.UserId = dto.UserId;
            up.DocId = dto.DocId;
            up.CanView = dto.CanView;
            up.CanUpload = dto.CanUpload;
            up.CanEdit = dto.CanEdit;
            up.CanDelete = dto.CanDelete;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(User, "UPDATE", "UserPermission", id.ToString(),
                $"UserPermission diperbarui untuk UserId={dto.UserId}, DocumentId={dto.DocId}");

            return NoContent();
        }

        // 📌 DELETE: api/UserPermission/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var up = await _context.UserPermissions.FindAsync(id);
            if (up == null) return NotFound();

            _context.UserPermissions.Remove(up);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(User, "DELETE", "UserPermission", id.ToString(),
                $"UserPermission dihapus (UserId={up.UserId}, DocumentId={up.DocId})");

            return NoContent();
        }
    }
}
