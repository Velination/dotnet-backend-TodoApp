using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using TodoApp.Data;
using TodoApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Connect to PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register password hashed
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add JWT Auth
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
        };
    });


builder.Services.AddAuthorization(); 

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Signup Endpoint
app.MapPost("/signup", async (
    UserSignupDto signupDto,
    AppDbContext db,
    IPasswordHasher<User> passwordHasher) =>
{
    // Check if email already exists
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == signupDto.Email);
    if (existingUser != null)
    {
        return Results.BadRequest(new { message = "Email already in use." });
    }

    // Create new user
    var user = new User
    {
        Email = signupDto.Email
    };

    // Hash the password before saving
    user.PasswordHash = passwordHasher.HashPassword(user, signupDto.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", new
    {
        message = "User created successfully",
        user = new { user.Id, user.Email }
    });
});


// login endpoint
app.MapPost("/login", async (
    UserLoginDto loginDto,
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IConfiguration config) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
    if (user == null)
    {
        return Results.BadRequest(new { message = "Invalid credentials" });
    }

    var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
    if (result == PasswordVerificationResult.Failed)
    {
        return Results.BadRequest(new { message = "Invalid credentials" });
    }

    // Generate JWT
    var jwtSettings = config.GetSection("Jwt");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
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

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        message = "Login successful",
        token
    });
});



// middleware calls 
app.UseAuthentication();
app.UseAuthorization();


app.Run();
