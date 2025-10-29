using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;

namespace WebArsip.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RoleController : ControllerBase
    {
        private readonly RoleManager<Role> _roleManager;

        public RoleController(RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
        }

        [HttpGet]
        public ActionResult<IEnumerable<RoleReadDto>> GetRoles()
        {
            var roles = _roleManager.Roles.ToList();

            return Ok(roles.Select(r => new RoleReadDto
            {
                RoleId = r.Id,
                RoleName = r.Name
            }).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();

            return new RoleReadDto
            {
                RoleId = role.Id,
                RoleName = role.Name
            };
        }

        [HttpGet("count")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> GetRoleCount()
        {
            var count = await _roleManager.Roles.CountAsync();
            return Ok(count);
        }

        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> CreateRole(RoleCreateDto dto)
        {
            var role = new Role
            {
                Name = dto.RoleName,
                NormalizedName = dto.RoleName.ToUpper()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new RoleReadDto
            {
                RoleId = role.Id,
                RoleName = role.Name
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, RoleCreateDto dto)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();

            role.Name = dto.RoleName;
            role.NormalizedName = dto.RoleName.ToUpper();

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return NoContent();
        }
    }
}
