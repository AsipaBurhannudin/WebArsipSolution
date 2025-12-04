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
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserController(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<UserReadDto>>> GetUsers([FromQuery] BaseQueryDto query)
        {
            var usersQuery = _userManager.Users;

            var totalCount = await usersQuery.CountAsync();

            var users = await usersQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var userDtos = new List<UserReadDto>();

            foreach (var u in users)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                var roleName = userRoles.FirstOrDefault() ?? "";

                userDtos.Add(new UserReadDto
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    RoleName = roleName,
                    IsActive = u.IsActive
                });
            }

            var result = new PagedResult<UserReadDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                Items = userDtos
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "";

            return new UserReadDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleName = roleName,
                IsActive = user.IsActive
            };
        }

        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto dto)
        {
            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var role = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
            if (role != null)
                await _userManager.AddToRoleAsync(user, role.Name);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserReadDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleName = role?.Name ?? ""
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.IsActive = dto.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Role handling aman
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (dto.RoleId > 0)
            {
                var newRole = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
                if (newRole != null)
                    await _userManager.AddToRoleAsync(user, newRole.Name);
            }
            else if (!currentRoles.Any())
            {
                // fallback default
                await _userManager.AddToRoleAsync(user, "Compliance");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetUserCount()
        {
            var count = await _userManager.Users.CountAsync();
            return Ok(count);
        }

        [HttpPost("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            // Hapus password lama
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Password berhasil direset" });
        }

    }
}