using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Controllers
{
    [ApiController]
   [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration config)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _config = config;
        }


       [HttpGet("protected-route")]
         public IActionResult ProtectedRoute()
        {
            return Ok(new { message = "You are authorized!" });
         }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserSignupDto signupDto)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == signupDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already in use." });
            }

              var user = new User
               {
                  Email = signupDto.Email,
                  FullName = signupDto.FullName, 
                  PasswordHash = _passwordHasher.HashPassword(null!, signupDto.Password)
                };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Created($"/users/{user.Id}", new
            {
                message = "User created successfully",
                user = new { user.Id, user.Email, user.FullName }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid credentials" });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return BadRequest(new { message = "Invalid credentials" });
            }

            var jwtSettings = _config.GetSection("Jwt");
           var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes(_config["Jwt:Key"] 
            ?? throw new InvalidOperationException("JWT Key not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiresInMinutes"])),
                signingCredentials: creds
            );

            

            return Ok(new
            {
                message = "Login successful",
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
    }
}
