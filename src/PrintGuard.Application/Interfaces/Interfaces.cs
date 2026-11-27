using PrintGuard.Application.DTOs;
using PrintGuard.Domain.Entities;

namespace PrintGuard.Application.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<Department> Departments { get; }
    IQueryable<User> Users { get; }
    IQueryable<AdminUser> AdminUsers { get; }
    IQueryable<Computer> Computers { get; }
    IQueryable<ClientDevice> ClientDevices { get; }
    IQueryable<Printer> Printers { get; }
    IQueryable<PrintQueue> PrintQueues { get; }
    IQueryable<UserSession> UserSessions { get; }
    IQueryable<PrintJob> PrintJobs { get; }
    IQueryable<PrintAuthorization> PrintAuthorizations { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<SystemSetting> SystemSettings { get; }
    IQueryable<PrintJobEvent> PrintJobEvents { get; }
    IQueryable<PrinterStatusLog> PrinterStatusLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<bool>> LogoutAsync(string sessionToken);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(long userId);
    Task<ApiResponse<AdminLoginResponse>> AdminLoginAsync(AdminLoginRequest request);
}

public interface IDeviceService
{
    Task<ApiResponse<RegisterDeviceResponse>> RegisterDeviceAsync(RegisterDeviceRequest request);
    Task<ApiResponse<bool>> HeartbeatAsync(HeartbeatRequest request);
    Task<ApiResponse<DeviceConfigResponse>> GetDeviceConfigAsync(long computerId, string deviceToken);
    Task<ApiResponse<bool>> RevokeDeviceAsync(long computerId);
}

public interface IPrintJobService
{
    Task<ApiResponse<PrintJobResponse>> RegisterPrintJobAsync(CreatePrintJobRequest request);
    Task<ApiResponse<PrintJobResponse>> GetPrintJobByIdAsync(Guid id);
    Task<ApiResponse<PrintJobResponse>> AuthorizePrintJobAsync(Guid jobId, AuthorizePrintJobRequest request);
    Task<ApiResponse<PrintJobResponse>> ReleasePrintJobAsync(Guid jobId);
    Task<ApiResponse<PrintJobResponse>> CancelPrintJobAsync(Guid jobId, string? reason);
    Task<ApiResponse<PrintJobResponse>> CompletePrintJobAsync(Guid jobId);
    Task<ApiResponse<PrintJobResponse>> FailPrintJobAsync(Guid jobId, string errorMessage);
}

public interface IAdminService
{
    Task<ApiResponse<DashboardSummaryResponse>> GetDashboardSummaryAsync();
    Task<ApiResponse<List<UserDto>>> GetUsersAsync(string? search, long? departmentId, bool? isActive);
    Task<ApiResponse<UserDto>> CreateUserAsync(UserCreateRequest request, long adminUserId);
    Task<ApiResponse<UserDto>> UpdateUserAsync(long id, UserUpdateRequest request, long adminUserId);
    Task<ApiResponse<bool>> ResetUserPasswordAsync(long id, string newPassword, long adminUserId);
    Task<ApiResponse<bool>> SetUserStatusAsync(long id, bool isActive, long adminUserId);
    Task<ApiResponse<List<PrinterDto>>> GetPrintersAsync();
    Task<ApiResponse<PrinterDto>> CreatePrinterAsync(PrinterDto dto, long adminUserId);
    Task<ApiResponse<PrinterDto>> UpdatePrinterAsync(long id, PrinterDto dto, long adminUserId);
    Task<ApiResponse<List<Computer>>> GetComputersAsync();
    Task<ApiResponse<List<PrintJobResponse>>> GetPrintHistoryAsync(DateTime? from, DateTime? to, long? userId, long? printerId, string? status);
    Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsAsync();
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IJwtTokenService
{
    string GenerateUserToken(User user);
    string GenerateAdminToken(AdminUser admin);
    string GenerateDeviceToken(long computerId, string hostname);
    string GenerateSessionToken(long userId, long computerId);
}

public interface IAuditService
{
    Task LogAsync(long? adminUserId, long? userId, string action, string? entityType, string? entityId, string? description, string? ipAddress = null);
}
