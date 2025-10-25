using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebArsip.Core.DTOs;
using WebArsip.Core.Entities;
using LoginResponse = WebArsip.Core.DTOs.LoginResponse;

namespace WebArsip.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IConfiguration _config;
        private readonly SignInManager<User> _signInManager;

        public AuthController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IConfiguration config,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
            _signInManager = signInManager;
        }

        // 🔹 LOGIN (respons JSON konsisten)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { success = false, message = "Email dan password wajib diisi." });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { success = false, message = "Email atau password salah." });

            if (!user.IsActive)
                return Unauthorized(new { success = false, message = "Akun Anda dinonaktifkan. Hubungi admin." });

            var checkPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!checkPassword)
                return Unauthorized(new { success = false, message = "Email atau password salah." });

            var roles = await _userManager.GetRolesAsync(user);

            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new
            {
                success = true,
                message = "Login berhasil!",
                data = new LoginResponse
                {
                    Token = jwt,
                    RoleName = string.Join(",", roles),
                    Email = user.Email
                }
            });
        }

        // 🔹 REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { success = false, message = "Email dan password wajib diisi." });

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return Ok(new { success = false, message = "Email sudah terdaftar." });

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return Ok(new
                {
                    success = false,
                    message = "Gagal mendaftarkan user.",
                    errors = result.Errors.Select(e => e.Description)
                });

            await _userManager.AddToRoleAsync(user, "Compliance");

            return Ok(new { success = true, message = "User berhasil didaftarkan." });
        }

        // 🔹 VERIFIKASI PASSWORD ADMIN (untuk fitur "Show Password")
        [HttpPost("verify-admin-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyAdminPassword([FromBody] AdminPasswordCheckDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { success = false, message = "Password tidak boleh kosong." });

            var adminEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(adminEmail))
                return Unauthorized(new { success = false, message = "Admin tidak terautentikasi." });

            var admin = await _userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
                return Unauthorized(new { success = false, message = "Akun admin tidak ditemukan." });

            // ✅ Gunakan _userManager.CheckPasswordAsync (lebih cepat)
            var isValid = await _userManager.CheckPasswordAsync(admin, dto.Password);

            if (!isValid)
                return Unauthorized(new { success = false, message = "Password admin salah." });

            return Ok(new { success = true, message = "Verifikasi berhasil." });
        }

    }

    // 🔸 DTO Tambahan
    public class AdminPasswordCheckDto
    {
        public string Password { get; set; } = string.Empty;
    }
}