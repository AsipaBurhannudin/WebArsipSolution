// WebArsip.Api.Controllers.SerialNumberController.cs
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
    public class SerialNumberController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SerialNumberService _svc;

        public SerialNumberController(AppDbContext db, SerialNumberService svc)
        {
            _db = db;
            _svc = svc;
        }

        // GET api/serialnumber
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.SerialNumberFormats.ToListAsync();

            var dto = list.Select(s => new SerialNumberFormatDto
            {
                Id = s.Id,
                Name = s.Name,
                Key = s.Key,
                Pattern = s.Pattern,
                CurrentNumber = s.CurrentNumber,
                IsActive = s.IsActive,
                Note = s.Note
            });

            return Ok(dto);
        }

        // CREATE format
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SerialNumberCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Key))
                return BadRequest(new { success = false, message = "Key required." });

            var exists = await _db.SerialNumberFormats.AnyAsync(x => x.Key == dto.Key);
            if (exists)
                return BadRequest(new { success = false, message = "Key already exists." });

            var e = new SerialNumberFormat
            {
                Name = dto.Name,
                Key = dto.Key,
                Pattern = dto.Pattern,
                CurrentNumber = dto.StartNumber,
                IsActive = dto.IsActive,
                Note = dto.Note
            };

            _db.Add(e);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, id = e.Id });
        }

        // UPDATE format
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SerialNumberFormatDto dto)
        {
            var e = await _db.SerialNumberFormats.FindAsync(id);
            if (e == null)
                return NotFound(new { success = false, message = "Not found." });

            e.Name = dto.Name;
            e.Key = dto.Key;
            e.Pattern = dto.Pattern;
            e.IsActive = dto.IsActive;
            e.CurrentNumber = dto.CurrentNumber;  // ← FIX UTAMA (selalu update)
            e.Note = dto.Note;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // DELETE format
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.SerialNumberFormats.FindAsync(id);
            if (e == null) return NotFound(new { success = false, message = "Not found." });

            _db.Remove(e);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ============================
        // PREVIEW — no increment
        // ============================
        [HttpPost("preview")]
        [AllowAnonymous]
        public async Task<IActionResult> Preview([FromBody] SerialNumberGenerateRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Key))
                return BadRequest(new { success = false, message = "Key is required." });

            var preview = await _svc.PreviewAsync(req.Key, req.Date);
            if (preview == null)
                return NotFound(new { success = false, message = "Format not found or inactive." });

            return Ok(new SerialNumberGenerateResponseDto
            {
                Success = true,
                Generated = preview,
                UsedNumber = 0
            });
        }

        // ============================
        // GENERATE — increment + save
        // ============================
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] SerialNumberGenerateRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Key))
                return BadRequest(new { success = false, message = "Key is required." });

            async Task<bool> UniquenessCheck(string candidate)
            {
                return !await _db.Documents.AnyAsync(d => d.Title == candidate);
            }

            var (ok, title, usedNumber) = await _svc.GenerateAsync(req.Key, UniquenessCheck);

            if (!ok)
                return StatusCode(500, new { success = false, message = "Failed to generate unique serial." });

            return Ok(new SerialNumberGenerateResponseDto
            {
                Success = true,
                Generated = title,
                UsedNumber = usedNumber
            });
        }
    }
}