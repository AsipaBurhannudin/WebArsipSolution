using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.DTOs;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // hanya admin yang bisa akses log
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 GET: api/auditlog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogReadDto>>> GetLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100) // ambil 100 terbaru
                .ToListAsync();

            return logs.Select(l => new AuditLogReadDto
            {
                AuditLogId = l.AuditLogId,
                UserId = l.UserId,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Timestamp = l.Timestamp,
                Details = l.Details
            }).ToList();
        }

        // 🔹 GET: api/auditlog/{id}
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
    }
}