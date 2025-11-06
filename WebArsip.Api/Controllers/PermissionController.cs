using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(AppDbContext context, IMemoryCache cache, ILogger<PermissionController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // ✅ 1️⃣ Endpoint untuk cek izin user/role — digunakan di MVC Index Document
        [HttpGet("check")]
        public async Task<IActionResult> CheckPermission([FromQuery] string email, [FromQuery] string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
                return BadRequest("Email and role are required.");

            try
            {
                string cacheKey = $"perm:{role}:{email}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var rolePerm = await _context.Permissions
                    .Include(p => p.Role)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Role != null && p.Role.Name == role);

                var userPerms = await _context.UserPermissions
                    .AsNoTracking()
                    .Where(up => up.UserEmail == email)
                    .ToListAsync();

                var result = new
                {
                    CanView = (rolePerm?.CanView ?? false) || userPerms.Any(p => p.CanView),
                    CanEdit = (rolePerm?.CanEdit ?? false) || userPerms.Any(p => p.CanEdit),
                    CanDelete = (rolePerm?.CanDelete ?? false) || userPerms.Any(p => p.CanDelete),
                    CanUpload = (rolePerm?.CanUpload ?? false) || userPerms.Any(p => p.CanUpload),
                    CanDownload = (rolePerm?.CanDownload ?? false) || userPerms.Any(p => p.CanDownload)
                };

                // cache 1 menit
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for {Email}", email);
                return StatusCode(500, "Internal Server Error while checking permission.");
            }
        }

        // ✅ 2️⃣ Endpoint untuk tampilkan daftar semua permission (hanya Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PermissionReadDto>>> GetPermissions()
        {
            var permissions = await _context.Permissions
                .Include(p => p.Role)
                .AsNoTracking()
                .ToListAsync();

            return permissions.Select(p => new PermissionReadDto
            {
                PermissionId = p.PermissionId,
                RoleId = p.RoleId,
                RoleName = p.Role.Name,
                CanView = p.CanView,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete,
                CanDownload = p.CanDownload,
                CanUpload = p.CanUpload
            }).ToList();
        }

        // ✅ 3️⃣ Create Permission (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PermissionReadDto>> CreatePermission(PermissionCreateDto dto)
        {
            var permission = new Permission
            {
                RoleId = dto.RoleId,
                CanView = dto.CanView,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete,
                CanDownload = dto.CanDownload,
                CanUpload = dto.CanUpload
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            var role = await _context.Roles.FindAsync(dto.RoleId);
            return new PermissionReadDto
            {
                PermissionId = permission.PermissionId,
                RoleId = dto.RoleId,
                RoleName = role?.Name ?? string.Empty,
                CanView = dto.CanView,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete,
                CanDownload = dto.CanDownload,
                CanUpload = dto.CanUpload
            };
        }

        // ✅ UPDATE Permission (Admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePermission(int id, PermissionCreateDto dto)
        {
            var permission = await _context.Permissions
                .Include(p => p.Role)
                .FirstOrDefaultAsync(p => p.PermissionId == id);

            if (permission == null) return NotFound();

            permission.RoleId = dto.RoleId;
            permission.CanView = dto.CanView;
            permission.CanEdit = dto.CanEdit;
            permission.CanDelete = dto.CanDelete;
            permission.CanDownload = dto.CanDownload;
            permission.CanUpload = dto.CanUpload;

            await _context.SaveChangesAsync();

            // 🧹 Bersihkan cache terkait role ini (biar frontend dapet izin terbaru)
            try
            {
                var roleName = permission.Role?.Name;
                if (!string.IsNullOrEmpty(roleName))
                {
                    var entriesField = typeof(MemoryCache).GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var entries = entriesField?.GetValue(_cache) as dynamic;
                    if (entries != null)
                    {
                        var keysToRemove = new List<object>();
                        foreach (var entry in entries)
                        {
                            var key = entry.GetType().GetProperty("Key")?.GetValue(entry, null)?.ToString();
                            if (key != null && key.Contains($"perm:{roleName}:"))
                                keysToRemove.Add(key);
                        }

                        foreach (var key in keysToRemove)
                            _cache.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CacheCleanError] {ex.Message}");
            }

            return NoContent();
        }

        // ✅ DELETE Permission (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions
                .Include(p => p.Role)
                .FirstOrDefaultAsync(p => p.PermissionId == id);

            if (permission == null) return NotFound();

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            // 🧹 Bersihkan cache terkait role ini juga
            try
            {
                var roleName = permission.Role?.Name;
                if (!string.IsNullOrEmpty(roleName))
                {
                    var entriesField = typeof(MemoryCache).GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var entries = entriesField?.GetValue(_cache) as dynamic;
                    if (entries != null)
                    {
                        var keysToRemove = new List<object>();
                        foreach (var entry in entries)
                        {
                            var key = entry.GetType().GetProperty("Key")?.GetValue(entry, null)?.ToString();
                            if (key != null && key.Contains($"perm:{roleName}:"))
                                keysToRemove.Add(key);
                        }

                        foreach (var key in keysToRemove)
                            _cache.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CacheCleanError] {ex.Message}");
            }

            return NoContent();
        }
    }
}