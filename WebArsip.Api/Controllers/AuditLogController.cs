using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.DTOs;
using WebArsip.Infrastructure.DbContexts;

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
                })
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
    }
}