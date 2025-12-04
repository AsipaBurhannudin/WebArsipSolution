using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.DTOs;
using WebArsip.Infrastructure.DbContexts;
using WebArsip.Core.DTOs.WebArsip.Core.DTOs;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<AuditLogReadDto>>> GetLogs([FromQuery] AuditLogQueryDto query)
        {
            var logsQuery = _context.AuditLogs.AsQueryable();

            // Filter tanggal
            if (query.From.HasValue)
            {
                var fromLocal = TimeZoneInfo.ConvertTimeToUtc(query.From.Value,
                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                logsQuery = logsQuery.Where(l => l.Timestamp >= fromLocal);
            }

            if (query.To.HasValue)
            {
                // tambahkan +1 hari supaya include semua log di hari "To"
                var toLocal = TimeZoneInfo.ConvertTimeToUtc(query.To.Value.AddDays(1),
                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                logsQuery = logsQuery.Where(l => l.Timestamp < toLocal);
            }

            if (!string.IsNullOrEmpty(query.UserId))
                logsQuery = logsQuery.Where(l => l.UserId == query.UserId);

            if (!string.IsNullOrEmpty(query.Action))
                logsQuery = logsQuery.Where(l => l.Action == query.Action);

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : (query.PageSize > 100 ? 100 : query.PageSize);

            var totalCount = await logsQuery.CountAsync();

            var logs = await logsQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ✅ Ambil semua user & dokumen untuk mapping cepat
            var users = await _context.Users.ToDictionaryAsync(u => u.Id.ToString(), u => u.Name);
            var documents = await _context.Documents.ToDictionaryAsync(d => d.DocId.ToString(), d => d.Title);

            var result = new PagedResult<AuditLogReadDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = logs.Select(l =>
                {
                    string userName = "(Unknown User)";
                    if (!string.IsNullOrEmpty(l.UserId) && users.ContainsKey(l.UserId))
                        userName = users[l.UserId];
                    else if (!string.IsNullOrEmpty(l.UserId))
                        userName = $"User ID: {l.UserId}";

                    string details = l.Details;
                    if (l.EntityName == "Document" && !string.IsNullOrEmpty(l.EntityId) && documents.ContainsKey(l.EntityId))
                        details = $"Document: {documents[l.EntityId]}";

                    return new AuditLogReadDto
                    {
                        AuditLogId = l.AuditLogId,
                        UserId = userName,
                        Action = l.Action,
                        EntityName = l.EntityName,
                        EntityId = l.EntityId,
                        Timestamp = l.Timestamp,
                        Details = details
                    };
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogReadDto>> GetLog(int id)
        {
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null) return NotFound();

            return new AuditLogReadDto
            {
                AuditLogId = log.AuditLogId,
                UserId = log.UserId,
                Action = log.Action,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Timestamp = log.Timestamp,
                Details = log.Details
            };
        }

        [HttpGet("daily-stats")]
        public async Task<ActionResult<IEnumerable<AuditLogRoleStatsDto>>> GetDailyStats(
    int days = 7)
        {
            var utcNow = DateTime.UtcNow;
            var startDate = utcNow.AddDays(-days);

            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            var logs = await _context.AuditLogs
                .Where(l => l.Timestamp >= startDate)
                .ToListAsync();

            var stats = logs
                .GroupBy(l => new
                {
                    l.UserId,
                    l.Action,
                    Date = TimeZoneInfo.ConvertTimeFromUtc(l.Timestamp, tz).Date
                })
                .Select(g => new AuditLogRoleStatsDto
                {
                    UserId = g.Key.UserId,
                    Action = g.Key.Action,
                    Count = g.Count(),
                    Date = g.Key.Date
                })
                .OrderBy(s => s.Date)
                .ToList();

            return Ok(stats);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAuditLogs([FromQuery] int limit = 1000)
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("AuditLogId,UserId,Action,EntityName,EntityId,Timestamp,Details");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.AuditLogId},{log.UserId},{log.Action},{log.EntityName},{log.EntityId},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Details}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "auditlogs.csv");
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetLogCount()
        {
            var count = await _context.AuditLogs.CountAsync();
            return Ok(count);
        }

        [AllowAnonymous]
        [HttpGet("user-activity")]
        public async Task<IActionResult> GetUserActivity([FromQuery] string? email, [FromQuery] int days = 7)
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";
            var startDate = DateTime.UtcNow.AddDays(-days);
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            var query = _context.AuditLogs
                .Where(l => l.Timestamp >= startDate)
                .AsQueryable();

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // User biasa hanya bisa lihat aktivitas dirinya
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Forbid();

                var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (userEntity == null)
                    return NotFound("User tidak ditemukan");

                query = query.Where(l => l.UserId == userEntity.Id.ToString());
            }
            else if (!string.IsNullOrEmpty(email))
            {
                // Admin bisa filter berdasarkan email user tertentu
                var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (userEntity != null)
                    query = query.Where(l => l.UserId == userEntity.Id.ToString());
            }

            var logs = await query.ToListAsync();

            var stats = logs
                .GroupBy(l => new
                {
                    Date = TimeZoneInfo.ConvertTimeFromUtc(l.Timestamp, tz).Date,
                    l.Action
                })
                .Select(g => new
                {
                    Date = g.Key.Date.ToString("yyyy-MM-dd"),
                    Action = g.Key.Action,
                    Count = g.Count()
                })
                .OrderBy(s => s.Date)
                .ToList();

            return Ok(stats);
        }

        /*[HttpGet("export/excel")]
        public async Task<IActionResult> ExportAuditLogsExcel([FromQuery] int limit = 1000)
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("AuditLogs");

            // Header
            worksheet.Cells[1, 1].Value = "AuditLogId";
            worksheet.Cells[1, 2].Value = "UserId";
            worksheet.Cells[1, 3].Value = "Action";
            worksheet.Cells[1, 4].Value = "EntityName";
            worksheet.Cells[1, 5].Value = "EntityId";
            worksheet.Cells[1, 6].Value = "Timestamp";
            worksheet.Cells[1, 7].Value = "Details";

            // Isi data
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                worksheet.Cells[i + 2, 1].Value = log.AuditLogId;
                worksheet.Cells[i + 2, 2].Value = log.UserId;
                worksheet.Cells[i + 2, 3].Value = log.Action;
                worksheet.Cells[i + 2, 4].Value = log.EntityName;
                worksheet.Cells[i + 2, 5].Value = log.EntityId;
                worksheet.Cells[i + 2, 6].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[i + 2, 7].Value = log.Details;
            }

            // Auto fit biar rapih
            worksheet.Cells.AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "auditlogs.xlsx");
        }*/
    }
}
