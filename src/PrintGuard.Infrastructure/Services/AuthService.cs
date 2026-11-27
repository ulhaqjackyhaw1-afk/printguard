using Microsoft.EntityFrameworkCore;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;
using PrintGuard.Infrastructure.Data;

namespace PrintGuard.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditService _audit;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ApiResponse<LoginResponse>.Fail("Invalid username or password");
        }

        if (!user.IsActive)
        {
            return ApiResponse<LoginResponse>.Fail("User account is inactive. Printing authorization denied.");
        }

        var computer = await _db.Computers.FindAsync(request.ComputerId);
        if (computer == null)
        {
            return ApiResponse<LoginResponse>.Fail("Unregistered computer");
        }

        var token = _jwt.GenerateUserToken(user);
        string? sessionToken = null;
        DateTime expiresAt = DateTime.UtcNow.AddHours(8);

        if (request.RememberMe)
        {
            sessionToken = _jwt.GenerateSessionToken(user.Id, computer.Id);
            var session = new UserSession
            {
                UserId = user.Id,
                ComputerId = computer.Id,
                TokenHash = BCrypt.Net.BCrypt.EnhancedHashPassword(sessionToken, 10),
                ExpiresAt = expiresAt
            };
            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(null, user.Id, "EMPLOYEE_LOGIN", "User", user.Id.ToString(), $"Employee logged in from {computer.Hostname}");

        var response = new LoginResponse(
            user.Id,
            user.FullName,
            user.Username,
            user.Department?.Name,
            token,
            sessionToken,
            expiresAt
        );

        return ApiResponse<LoginResponse>.Ok(response, "Login successful");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string sessionToken)
    {
        // Mark session as revoked
        var activeSessions = await _db.UserSessions
            .Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var s in activeSessions)
        {
            if (BCrypt.Net.BCrypt.EnhancedVerify(sessionToken, s.TokenHash))
            {
                s.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _audit.LogAsync(null, s.UserId, "EMPLOYEE_LOGOUT", "UserSession", s.Id.ToString(), "Session revoked on logout");
                return ApiResponse<bool>.Ok(true, "Logout successful");
            }
        }

        return ApiResponse<bool>.Ok(true, "Session closed");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(long userId)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return ApiResponse<UserDto>.Fail("User not found");

        var dto = new UserDto(
            user.Id,
            user.EmployeeId,
            user.FullName,
            user.Username,
            user.Email,
            user.Department?.Name,
            user.DepartmentId,
            user.Position,
            user.IsActive,
            user.CreatedAt
        );

        return ApiResponse<UserDto>.Ok(dto);
    }

    public async Task<ApiResponse<AdminLoginResponse>> AdminLoginAsync(AdminLoginRequest request)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Username == request.Username);
        if (admin == null || !_hasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            return ApiResponse<AdminLoginResponse>.Fail("Invalid admin credentials");
        }

        if (!admin.IsActive)
        {
            return ApiResponse<AdminLoginResponse>.Fail("Admin account is deactivated");
        }

        var token = _jwt.GenerateAdminToken(admin);
        await _audit.LogAsync(admin.Id, null, "ADMIN_LOGIN", "AdminUser", admin.Id.ToString(), "Admin logged into dashboard");

        var response = new AdminLoginResponse(
            admin.Id,
            admin.FullName,
            admin.Username,
            admin.Role,
            token
        );

        return ApiResponse<AdminLoginResponse>.Ok(response, "Admin login successful");
    }
}
