using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.Entities;
using WebArsip.Core.DTOs;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DocumentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/document
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentReadDto>>> GetDocuments()
        {
            var docs = await _context.Documents.ToListAsync();

            return docs.Select(d => new DocumentReadDto
            {
                DocId = d.DocId,
                Title = d.Title,
                Description = d.Description,
                FilePath = d.FilePath,
                CreatedDate = d.CreatedDate,
                Status = d.Status
            }).ToList();
        }

        // GET: api/document/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentReadDto>> GetDocument(int id)
        {
            var d = await _context.Documents.FindAsync(id);

            if (d == null)
                return NotFound();

            return new DocumentReadDto
            {
                DocId = d.DocId,
                Title = d.Title,
                Description = d.Description,
                FilePath = d.FilePath,
                CreatedDate = d.CreatedDate,
                Status = d.Status
            };
        }

        // POST: api/document
        [HttpPost]
        public async Task<ActionResult<DocumentReadDto>> CreateDocument(DocumentCreateDto dto)
        {
            var doc = new Document
            {
                Title = dto.Title,
                Description = dto.Description,
                FilePath = dto.FilePath,
                CreatedDate = DateTime.UtcNow,
                Status = dto.Status
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var result = new DocumentReadDto
            {
                DocId = doc.DocId,
                Title = doc.Title,
                Description = doc.Description,
                FilePath = doc.FilePath,
                CreatedDate = doc.CreatedDate,
                Status = doc.Status
            };

            return CreatedAtAction(nameof(GetDocument), new { id = doc.DocId }, result);
        }

        // PUT: api/document/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, DocumentUpdateDto dto)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                return NotFound();

            doc.Title = dto.Title;
            doc.Description = dto.Description;
            doc.FilePath = dto.FilePath;
            doc.Status = dto.Status;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/document/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                return NotFound();

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}