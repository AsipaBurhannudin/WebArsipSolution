using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;
using static WebArsip.Core.DTOs.RoleCreateDto;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleReadDto>>> GetRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            return roles.Select(r => new RoleReadDto { RoleId = r.RoleId, RoleName = r.RoleName }).ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            return new RoleReadDto { RoleId = role.RoleId, RoleName = role.RoleName };
        }

        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> CreateRole(RoleCreateDto dto)
        {
            var role = new Role { RoleName = dto.RoleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRole), new { id = role.RoleId },
                new RoleReadDto { RoleId = role.RoleId, RoleName = role.RoleName });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, RoleCreateDto dto)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            role.RoleName = dto.RoleName;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
