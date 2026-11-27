using Microsoft.EntityFrameworkCore;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;
using PrintGuard.Infrastructure.Data;

namespace PrintGuard.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;

    public AdminService(ApplicationDbContext db, IPasswordHasher hasher, IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<ApiResponse<DashboardSummaryResponse>> GetDashboardSummaryAsync()
    {
        var totalEmployees = await _db.Users.CountAsync();
        var totalPrinters = await _db.Printers.CountAsync(p => p.IsActive);
        var onlineThreshold = DateTime.UtcNow.AddMinutes(-5);
        var onlineComputers = await _db.Computers.CountAsync(c => c.Status == "ONLINE" && c.LastSeenAt >= onlineThreshold);

        var totalJobs = await _db.PrintJobs.CountAsync();
        var totalPages = await _db.PrintJobs.Where(j => j.Status == "COMPLETED").SumAsync(j => (int?)j.TotalPages * j.Copies) ?? 0;
        var colorPages = await _db.PrintJobs.Where(j => j.Status == "COMPLETED" && j.ColorMode == "COLOR").SumAsync(j => (int?)j.TotalPages * j.Copies) ?? 0;
        var monoPages = totalPages - colorPages;

        // Daily activity (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
        var recentJobs = await _db.PrintJobs
            .Where(j => j.SubmittedAt >= sevenDaysAgo)
            .ToListAsync();

        var dailyActivity = recentJobs
            .GroupBy(j => j.SubmittedAt.ToString("yyyy-MM-dd"))
            .Select(g => new DailyPrintStat(
                g.Key,
                g.Count(),
                g.Sum(j => j.TotalPages * j.Copies)
            ))
            .OrderBy(d => d.Date)
            .ToList();

        // Top Employees
        var topEmployees = await _db.PrintJobs
            .Where(j => j.UserId != null && j.Status == "COMPLETED")
            .Include(j => j.User)
            .ThenInclude(u => u!.Department)
            .GroupBy(j => new { j.UserId, j.User!.FullName, Dept = j.User.Department != null ? j.User.Department.Name : "N/A" })
            .Select(g => new TopUserStat(
                g.Key.FullName,
                g.Key.Dept,
                g.Count(),
                g.Sum(j => j.TotalPages * j.Copies)
            ))
            .OrderByDescending(t => t.TotalPages)
            .Take(5)
            .ToListAsync();

        // Most Used Printers
        var topPrinters = await _db.PrintJobs
            .Include(j => j.Printer)
            .GroupBy(j => j.Printer.Name)
            .Select(g => new PrinterUsageStat(
                g.Key,
                g.Count(),
                g.Sum(j => j.TotalPages * j.Copies)
            ))
            .OrderByDescending(p => p.TotalJobs)
            .Take(5)
            .ToListAsync();

        var summary = new DashboardSummaryResponse(
            totalEmployees,
            totalPrinters,
            onlineComputers,
            totalJobs,
            totalPages,
            colorPages,
            monoPages,
            dailyActivity,
            topEmployees,
            topPrinters
        );

        return ApiResponse<DashboardSummaryResponse>.Ok(summary);
    }

    public async Task<ApiResponse<List<UserDto>>> GetUsersAsync(string? search, long? departmentId, bool? isActive)
    {
        var query = _db.Users.Include(u => u.Department).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s) || u.Username.ToLower().Contains(s) || (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(s)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(u => u.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var list = await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(
                u.Id,
                u.EmployeeId,
                u.FullName,
                u.Username,
                u.Email,
                u.Department != null ? u.Department.Name : null,
                u.DepartmentId,
                u.Position,
                u.IsActive,
                u.CreatedAt
            ))
            .ToListAsync();

        return ApiResponse<List<UserDto>>.Ok(list);
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(UserCreateRequest request, long adminUserId)
    {
        var existing = await _db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
        if (existing)
            return ApiResponse<UserDto>.Fail("Username is already taken");

        var user = new User
        {
            EmployeeId = request.EmployeeId,
            FullName = request.FullName,
            Username = request.Username,
            PasswordHash = _hasher.HashPassword(request.Password),
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            Position = request.Position,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(adminUserId, user.Id, "CREATE_USER", "User", user.Id.ToString(), $"Admin created user {user.Username}");

        var dept = user.DepartmentId.HasValue ? await _db.Departments.FindAsync(user.DepartmentId.Value) : null;
        var dto = new UserDto(user.Id, user.EmployeeId, user.FullName, user.Username, user.Email, dept?.Name, user.DepartmentId, user.Position, user.IsActive, user.CreatedAt);
        return ApiResponse<UserDto>.Ok(dto, "User created successfully");
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(long id, UserUpdateRequest request, long adminUserId)
    {
        var user = await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return ApiResponse<UserDto>.Fail("User not found");

        user.EmployeeId = request.EmployeeId;
        user.FullName = request.FullName;
        user.Email = request.Email;
        user.DepartmentId = request.DepartmentId;
        user.Position = request.Position;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, user.Id, "UPDATE_USER", "User", user.Id.ToString(), $"Admin updated user {user.Username}");

        var dept = user.DepartmentId.HasValue ? await _db.Departments.FindAsync(user.DepartmentId.Value) : null;
        var dto = new UserDto(user.Id, user.EmployeeId, user.FullName, user.Username, user.Email, dept?.Name, user.DepartmentId, user.Position, user.IsActive, user.CreatedAt);
        return ApiResponse<UserDto>.Ok(dto, "User updated successfully");
    }

    public async Task<ApiResponse<bool>> ResetUserPasswordAsync(long id, string newPassword, long adminUserId)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResponse<bool>.Fail("User not found");

        user.PasswordHash = _hasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke active sessions
        var sessions = await _db.UserSessions.Where(s => s.UserId == id && s.RevokedAt == null).ToListAsync();
        foreach (var s in sessions)
            s.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, user.Id, "RESET_PASSWORD", "User", user.Id.ToString(), $"Admin reset password for user {user.Username}");

        return ApiResponse<bool>.Ok(true, "Password reset successfully");
    }

    public async Task<ApiResponse<bool>> SetUserStatusAsync(long id, bool isActive, long adminUserId)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResponse<bool>.Fail("User not found");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!isActive)
        {
            var sessions = await _db.UserSessions.Where(s => s.UserId == id && s.RevokedAt == null).ToListAsync();
            foreach (var s in sessions)
                s.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, user.Id, "SET_USER_STATUS", "User", user.Id.ToString(), $"User status changed to {(isActive ? "ACTIVE" : "INACTIVE")}");

        return ApiResponse<bool>.Ok(true, $"User is now {(isActive ? "active" : "inactive")}");
    }

    public async Task<ApiResponse<List<PrinterDto>>> GetPrintersAsync()
    {
        var printers = await _db.Printers
            .Include(p => p.PrintJobs)
            .OrderBy(p => p.Name)
            .Select(p => new PrinterDto(
                p.Id,
                p.Name,
                p.Model,
                p.IpAddress,
                p.Location,
                p.PrinterServer,
                p.IsActive,
                p.PrintJobs.Count,
                p.PrintJobs.Where(j => j.Status == "COMPLETED").Sum(j => j.TotalPages * j.Copies)
            ))
            .ToListAsync();

        return ApiResponse<List<PrinterDto>>.Ok(printers);
    }

    public async Task<ApiResponse<PrinterDto>> CreatePrinterAsync(PrinterDto dto, long adminUserId)
    {
        var printer = new Printer
        {
            Name = dto.Name,
            Model = dto.Model,
            IpAddress = dto.IpAddress,
            Location = dto.Location,
            PrinterServer = dto.PrinterServer,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Printers.Add(printer);
        await _db.SaveChangesAsync();

        _db.PrintQueues.Add(new PrintQueue
        {
            PrinterId = printer.Id,
            QueueName = $"{printer.Name}-HOLD",
            QueueType = "HOLD",
            IsActive = true
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, null, "CREATE_PRINTER", "Printer", printer.Id.ToString(), $"Admin created printer {printer.Name}");

        return ApiResponse<PrinterDto>.Ok(dto with { Id = printer.Id });
    }

    public async Task<ApiResponse<PrinterDto>> UpdatePrinterAsync(long id, PrinterDto dto, long adminUserId)
    {
        var printer = await _db.Printers.FindAsync(id);
        if (printer == null)
            return ApiResponse<PrinterDto>.Fail("Printer not found");

        printer.Name = dto.Name;
        printer.Model = dto.Model;
        printer.IpAddress = dto.IpAddress;
        printer.Location = dto.Location;
        printer.PrinterServer = dto.PrinterServer;
        printer.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, null, "UPDATE_PRINTER", "Printer", printer.Id.ToString(), $"Admin updated printer {printer.Name}");

        return ApiResponse<PrinterDto>.Ok(dto);
    }

    public async Task<ApiResponse<List<Computer>>> GetComputersAsync()
    {
        var computers = await _db.Computers
            .OrderByDescending(c => c.LastSeenAt)
            .ToListAsync();

        return ApiResponse<List<Computer>>.Ok(computers);
    }

    public async Task<ApiResponse<List<PrintJobResponse>>> GetPrintHistoryAsync(DateTime? from, DateTime? to, long? userId, long? printerId, string? status)
    {
        var query = _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .Include(j => j.User)
            .ThenInclude(u => u!.Department)
            .AsQueryable();

        if (from.HasValue) query = query.Where(j => j.SubmittedAt >= from.Value);
        if (to.HasValue) query = query.Where(j => j.SubmittedAt <= to.Value);
        if (userId.HasValue) query = query.Where(j => j.UserId == userId.Value);
        if (printerId.HasValue) query = query.Where(j => j.PrinterId == printerId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(j => j.Status == status);

        var list = await query
            .OrderByDescending(j => j.SubmittedAt)
            .Take(200)
            .Select(j => new PrintJobResponse(
                j.Id,
                j.ServerJobId,
                j.UserId,
                j.User != null ? j.User.FullName : null,
                j.User != null && j.User.Department != null ? j.User.Department.Name : null,
                j.ComputerId,
                j.Computer != null ? j.Computer.Hostname : null,
                j.PrinterId,
                j.Printer.Name,
                j.DocumentName,
                j.TotalPages,
                j.Copies,
                j.ColorMode,
                j.Status,
                j.SubmittedAt,
                j.AuthorizedAt,
                j.CompletedAt,
                j.ErrorMessage
            ))
            .ToListAsync();

        return ApiResponse<List<PrintJobResponse>>.Ok(list);
    }

    public async Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsAsync()
    {
        var logs = await _db.AuditLogs
            .Include(a => a.AdminUser)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new AuditLogDto(
                a.Id,
                a.AdminUser != null ? a.AdminUser.FullName : null,
                a.User != null ? a.User.FullName : null,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Description,
                a.IpAddress,
                a.CreatedAt
            ))
            .ToListAsync();

        return ApiResponse<List<AuditLogDto>>.Ok(logs);
    }
}
