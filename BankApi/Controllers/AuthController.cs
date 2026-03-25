using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BankApi.Data;
using BankApi.DTOs;
using BankApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private bool IsValidEmail(string email)
        {
            var pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            return Regex.IsMatch(email, pattern);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "ข้อมูลไม่ถูกต้อง" });

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email และ Password ห้ามว่าง" });

            var normalizedEmail = dto.Email.Trim().ToLower();

            if (!IsValidEmail(normalizedEmail))
                return BadRequest(new { message = "รูปแบบ Email ไม่ถูกต้อง" });

            if (dto.Password.Length < 4)
                return BadRequest(new { message = "Password ต้องมีอย่างน้อย 4 ตัวอักษร" });

            if (dto.InitialBalance < 0)
                return BadRequest(new { message = "InitialBalance ห้ามน้อยกว่า 0" });

            var existingUser = await _context.Users.AnyAsync(x => x.Email == normalizedEmail);
            if (existingUser)
                return BadRequest(new { message = "Email นี้ถูกใช้งานแล้ว" });

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                InitialBalance = dto.InitialBalance,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "สร้างผู้ใช้สำเร็จ",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.InitialBalance,
                    user.CreatedAt
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "ข้อมูลไม่ถูกต้อง" });

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email และ Password ห้ามว่าง" });

            var normalizedEmail = dto.Email.Trim().ToLower();

            if (!IsValidEmail(normalizedEmail))
                return BadRequest(new { message = "รูปแบบ Email ไม่ถูกต้อง" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

            if (user == null)
                return Unauthorized(new { message = "Email หรือ Password ไม่ถูกต้อง" });

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!isPasswordValid)
                return Unauthorized(new { message = "Email หรือ Password ไม่ถูกต้อง" });

            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                return StatusCode(500, new { message = "JWT Key ไม่ถูกต้อง" });

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "เข้าสู่ระบบสำเร็จ",
                email = user.Email,
                token = jwt,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.InitialBalance
                }
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new
            {
                id,
                email
            });
        }
    }
}