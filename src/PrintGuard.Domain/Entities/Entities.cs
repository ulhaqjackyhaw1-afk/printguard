using System.Net;
using PrintGuard.Domain.Enums;

namespace PrintGuard.Domain.Entities;

public class Department
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User
{
    public long Id { get; set; }
    public long? DepartmentId { get; set; }
    public string? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Department? Department { get; set; }
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();
}

public class AdminUser
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "ADMIN";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Computer
{
    public long Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? WindowsVersion { get; set; }
    public string? AgentVersion { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string Status { get; set; } = "OFFLINE"; // ONLINE, OFFLINE
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClientDevice> ClientDevices { get; set; } = new List<ClientDevice>();
    public ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();
}

public class ClientDevice
{
    public long Id { get; set; }
    public long ComputerId { get; set; }
    public string DeviceTokenHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public Computer Computer { get; set; } = null!;
}

public class Printer
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public string? PrinterServer { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PrintQueue> Queues { get; set; } = new List<PrintQueue>();
    public ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();
}

public class PrintQueue
{
    public long Id { get; set; }
    public long PrinterId { get; set; }
    public string QueueName { get; set; } = string.Empty;
    public string QueueType { get; set; } = "HOLD"; // HOLD, RELEASE, DIRECT
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Printer Printer { get; set; } = null!;
}

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long ComputerId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Computer Computer { get; set; } = null!;
}

public class PrintJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? ServerJobId { get; set; }
    public long? UserId { get; set; }
    public long? ComputerId { get; set; }
    public long PrinterId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int TotalPages { get; set; } = 1;
    public int Copies { get; set; } = 1;
    public string ColorMode { get; set; } = "MONO"; // COLOR, MONO
    public string Status { get; set; } = PrintJobStatus.RECEIVED.ToString();
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public User? User { get; set; }
    public Computer? Computer { get; set; }
    public Printer Printer { get; set; } = null!;
    public ICollection<PrintAuthorization> Authorizations { get; set; } = new List<PrintAuthorization>();
    public ICollection<PrintJobEvent> Events { get; set; } = new List<PrintJobEvent>();
}

public class PrintAuthorization
{
    public long Id { get; set; }
    public Guid PrintJobId { get; set; }
    public long UserId { get; set; }
    public long ComputerId { get; set; }
    public string Result { get; set; } = string.Empty; // SUCCESS, DENIED, EXPIRED
    public string AuthMethod { get; set; } = string.Empty; // PASSWORD, REMEMBER_ME
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PrintJob PrintJob { get; set; } = null!;
    public User User { get; set; } = null!;
    public Computer Computer { get; set; } = null!;
}

public class AuditLog
{
    public long Id { get; set; }
    public long? AdminUserId { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AdminUser? AdminUser { get; set; }
    public User? User { get; set; }
}

public class SystemSetting
{
    public long Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = "{}"; // JSON string
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PrintJobEvent
{
    public long Id { get; set; }
    public Guid PrintJobId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PrintJob PrintJob { get; set; } = null!;
}

public class PrinterStatusLog
{
    public long Id { get; set; }
    public long PrinterId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Printer Printer { get; set; } = null!;
}
