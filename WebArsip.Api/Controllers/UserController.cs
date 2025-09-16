using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
        {
            var users = _userManager.Users.ToList();
            var roles = _roleManager.Roles.ToList();

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
                    RoleName = roleName
                });
            }

            return Ok(userDtos);
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
                RoleName = roleName
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

            // Tambah role
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

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var newRole = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
            if (newRole != null)
                await _userManager.AddToRoleAsync(user, newRole.Name);

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
    }
}