namespace PrintGuard.Application.DTOs;

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data = default,
    object? Errors = null,
    DateTime? Timestamp = null
)
{
    public static ApiResponse<T> Ok(T data, string message = "Request successful") =>
        new(true, message, data, null, DateTime.UtcNow);

    public static ApiResponse<T> Fail(string message, object? errors = null) =>
        new(false, message, default, errors, DateTime.UtcNow);
}

// ---------------- AUTH DTOs ----------------
public record LoginRequest(
    string Username,
    string Password,
    long ComputerId,
    bool RememberMe = false
);

public record LoginResponse(
    long UserId,
    string FullName,
    string Username,
    string? Department,
    string Token,
    string? SessionToken,
    DateTime ExpiresAt
);

public record AdminLoginRequest(
    string Username,
    string Password
);

public record AdminLoginResponse(
    long AdminId,
    string FullName,
    string Username,
    string Role,
    string Token
);

// ---------------- DEVICE DTOs ----------------
public record RegisterDeviceRequest(
    string Hostname,
    string? IpAddress,
    string? WindowsVersion,
    string? AgentVersion
);

public record RegisterDeviceResponse(
    long ComputerId,
    string Hostname,
    string DeviceToken,
    string Status
);

public record HeartbeatRequest(
    long ComputerId,
    string DeviceToken,
    string? AgentVersion,
    string? IpAddress
);

public record DeviceConfigResponse(
    int RememberMeDurationHours,
    bool RequireAuthEveryJob,
    int HeartbeatIntervalSeconds,
    List<PrinterDto> AvailablePrinters
);

// ---------------- PRINT JOB DTOs ----------------
public record CreatePrintJobRequest(
    string ServerJobId,
    long ComputerId,
    long PrinterId,
    string DocumentName,
    int TotalPages = 1,
    int Copies = 1,
    string ColorMode = "MONO",
    string? PrinterName = null
);

public record AuthorizePrintJobRequest(
    string? Username,
    string? Password,
    string? SessionToken,
    long ComputerId,
    bool RememberMe = false
);

public record PrintJobResponse(
    Guid Id,
    string? ServerJobId,
    long? UserId,
    string? UserName,
    string? DepartmentName,
    long? ComputerId,
    string? ComputerHostname,
    long PrinterId,
    string PrinterName,
    string DocumentName,
    int TotalPages,
    int Copies,
    string ColorMode,
    string Status,
    DateTime SubmittedAt,
    DateTime? AuthorizedAt,
    DateTime? CompletedAt,
    string? ErrorMessage
);

public record PrintJobStatusUpdateRequest(
    string Status,
    string? Details
);

// ---------------- ADMIN / PRINTER / USER DTOs ----------------
public record PrinterDto(
    long Id,
    string Name,
    string? Model,
    string? IpAddress,
    string? Location,
    string? PrinterServer,
    bool IsActive,
    int TotalJobs = 0,
    int TotalPages = 0
);

public record UserDto(
    long Id,
    string? EmployeeId,
    string FullName,
    string Username,
    string? Email,
    string? Department,
    long? DepartmentId,
    string? Position,
    bool IsActive,
    DateTime CreatedAt
);

public record UserCreateRequest(
    string? EmployeeId,
    string FullName,
    string Username,
    string Password,
    string? Email,
    long? DepartmentId,
    string? Position
);

public record UserUpdateRequest(
    string? EmployeeId,
    string FullName,
    string? Email,
    long? DepartmentId,
    string? Position,
    bool IsActive
);

public record DashboardSummaryResponse(
    int TotalEmployees,
    int TotalPrinters,
    int OnlineComputers,
    int TotalPrintJobs,
    int TotalPrintedPages,
    int ColorPages,
    int MonochromePages,
    List<DailyPrintStat> DailyActivity,
    List<TopUserStat> TopEmployees,
    List<PrinterUsageStat> MostUsedPrinters
);

public record DailyPrintStat(string Date, int TotalJobs, int TotalPages);
public record TopUserStat(string FullName, string Department, int TotalJobs, int TotalPages);
public record PrinterUsageStat(string PrinterName, int TotalJobs, int TotalPages);
public record AuditLogDto(
    long Id,
    string? AdminName,
    string? UserName,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Description,
    string? IpAddress,
    DateTime CreatedAt
);
