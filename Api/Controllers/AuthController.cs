using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FRAServiceRequestPortal.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FRAServiceRequestPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult Login([FromBody] LoginRequest request)
    {
        var users = _configuration.GetSection("AuthUsers").Get<List<AuthUser>>() ?? new List<AuthUser>();
        var user = users.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password);

        if (user is null)
        {
            return Unauthorized();
        }

        var tokenResponse = CreateToken(user);
        return Ok(tokenResponse);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult GetMe()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        return Ok(new
        {
            email,
            roles
        });
    }

    private object CreateToken(AuthUser user)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? string.Empty;
        var audience = _configuration["Jwt:Audience"] ?? string.Empty;
        var key = _configuration["Jwt:Key"] ?? string.Empty;
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var minutes)
            ? minutes
            : int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var legacyMinutes)
                ? legacyMinutes
                : 60;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Email)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            email = user.Email,
            roles = user.Roles
        };
    }

    private sealed class AuthUser
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public List<string> Roles { get; init; } = new();
    }
}
