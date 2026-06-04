using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NickPOS.Backend.Data;
using NickPOS.Backend.DTOs;
using NickPOS.Backend.Models;
using Microsoft.AspNetCore.Identity;
using NuGet.Protocol;

namespace NickPOS.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public UserController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            var user = await AuthenticateUser(login);

            if (user == null)
            {
                return Unauthorized();
            }

            var tokenString = GenerateJSONWebToken(user);

            return Ok(new
            {
                token = tokenString
            });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == request.Username ||
                    u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest("Username or email already exists.");
            }

            var passwordHasher = new PasswordHasher<UserModel>();

            var user = new UserModel
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email
                // ,Role = "User"
            };

            if (!string.IsNullOrEmpty(request.Role))
            {
                user.Role = request.Role; // only if you trust the caller (NOT recommended publicly)
            }
            else
            {
                user.Role = AppRoles.User;
            }

            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully."
            });
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully." });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
                return NotFound();

            user.FullName = request.FullName;
            user.Email = request.Email;

            await _context.SaveChangesAsync();

            return Ok(new ProfileResponse
            {
                Username = user.Username ?? "",
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                Role = user.Role ?? ""
            });
        }

        [Authorize]
        [HttpPut("me/password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
                return NotFound();

            var hasher = new PasswordHasher<UserModel>();

            var verify = hasher.VerifyHashedPassword(
                user,
                user.PasswordHash!,
                request.CurrentPassword);

            if (verify != PasswordVerificationResult.Success)
                return BadRequest("Current password is incorrect");

            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully" });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

             var response = new ProfileResponse
            {
                Username = user.Username ?? "",
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                Role = user.Role ?? ""
            };
            
            return Ok(response);
        }
        
        private string GenerateJSONWebToken(UserModel user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? AppRoles.User)
            };

            // this allows to have api endpoints as [Authorize(Roles = AppRoles.User)] or whatever kind of role it is
            // or if multiple roles: [Authorize(Roles = $"{AppRoles.Manager},{AppRoles.Admin}")]

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserModel?> AuthenticateUser(LoginRequest login)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == login.Username);

            if (user == null)
            {
                return null;
            }

            var passwordHasher = new PasswordHasher<UserModel>();

            var result = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash!,
                login.Password);

            return result == PasswordVerificationResult.Success
                ? user
                : null;
        }

        private long? GetUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(id, out var parsed) ? parsed : null;
        }
    }
}
