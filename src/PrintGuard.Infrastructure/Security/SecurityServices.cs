using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;

namespace PrintGuard.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 11);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly string _jwtKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
        _jwtKey = _config["Jwt:Key"] ?? "PrintGuardSecretKeyMustBe32CharactersOrMore_SuperSecure2026";
        _issuer = _config["Jwt:Issuer"] ?? "PrintGuardServer";
        _audience = _config["Jwt:Audience"] ?? "PrintGuardClients";
    }

    public string GenerateUserToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim("role", "EMPLOYEE"),
            new Claim("department_id", user.DepartmentId?.ToString() ?? "")
        };

        return BuildToken(claims, DateTime.UtcNow.AddHours(8));
    }

    public string GenerateAdminToken(AdminUser admin)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.GivenName, admin.FullName),
            new Claim(ClaimTypes.Role, admin.Role)
        };

        return BuildToken(claims, DateTime.UtcNow.AddHours(12));
    }

    public string GenerateDeviceToken(long computerId, string hostname)
    {
        var claims = new[]
        {
            new Claim("computer_id", computerId.ToString()),
            new Claim("hostname", hostname),
            new Claim("type", "DEVICE")
        };

        return BuildToken(claims, DateTime.UtcNow.AddYears(1));
    }

    public string GenerateSessionToken(long userId, long computerId)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string BuildToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
