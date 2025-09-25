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

            if (!query.From.HasValue && !query.To.HasValue)
            {
                var defaultFrom = DateTime.UtcNow.AddDays(-30);
                logsQuery = logsQuery.Where(l => l.Timestamp >= defaultFrom);
            }
            else
            {
                if (query.From.HasValue)
                    logsQuery = logsQuery.Where(l => l.Timestamp >= query.From.Value);

                if (query.To.HasValue)
                    logsQuery = logsQuery.Where(l => l.Timestamp <= query.To.Value);
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

            var result = new PagedResult<AuditLogReadDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = logs.Select(l => new AuditLogReadDto
                {
                    AuditLogId = l.AuditLogId,
                    UserId = l.UserId,
                    Action = l.Action,
                    EntityName = l.EntityName,
                    EntityId = l.EntityId,
                    Timestamp = l.Timestamp,
                    Details = l.Details
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
