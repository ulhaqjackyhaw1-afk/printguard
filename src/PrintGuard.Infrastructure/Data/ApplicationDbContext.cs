using Microsoft.EntityFrameworkCore;
using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;

namespace PrintGuard.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<ClientDevice> ClientDevices => Set<ClientDevice>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<PrintQueue> PrintQueues => Set<PrintQueue>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<PrintAuthorization> PrintAuthorizations => Set<PrintAuthorization>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PrintJobEvent> PrintJobEvents => Set<PrintJobEvent>();
    public DbSet<PrinterStatusLog> PrinterStatusLogs => Set<PrinterStatusLog>();

    IQueryable<Department> IApplicationDbContext.Departments => Departments;
    IQueryable<User> IApplicationDbContext.Users => Users;
    IQueryable<AdminUser> IApplicationDbContext.AdminUsers => AdminUsers;
    IQueryable<Computer> IApplicationDbContext.Computers => Computers;
    IQueryable<ClientDevice> IApplicationDbContext.ClientDevices => ClientDevices;
    IQueryable<Printer> IApplicationDbContext.Printers => Printers;
    IQueryable<PrintQueue> IApplicationDbContext.PrintQueues => PrintQueues;
    IQueryable<UserSession> IApplicationDbContext.UserSessions => UserSessions;
    IQueryable<PrintJob> IApplicationDbContext.PrintJobs => PrintJobs;
    IQueryable<PrintAuthorization> IApplicationDbContext.PrintAuthorizations => PrintAuthorizations;
    IQueryable<AuditLog> IApplicationDbContext.AuditLogs => AuditLogs;
    IQueryable<SystemSetting> IApplicationDbContext.SystemSettings => SystemSettings;
    IQueryable<PrintJobEvent> IApplicationDbContext.PrintJobEvents => PrintJobEvents;
    IQueryable<PrinterStatusLog> IApplicationDbContext.PrinterStatusLogs => PrinterStatusLogs;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Departments
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id").HasMaxLength(50);
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(150);
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(80);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150);
            entity.Property(e => e.Position).HasColumnName("position").HasMaxLength(100);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.EmployeeId).IsUnique();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Admin Users
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(150);
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(80);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(30);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Computers
        modelBuilder.Entity<Computer>(entity =>
        {
            entity.ToTable("computers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Hostname).HasColumnName("hostname").IsRequired().HasMaxLength(120);
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.WindowsVersion).HasColumnName("windows_version").HasMaxLength(100);
            entity.Property(e => e.AgentVersion).HasColumnName("agent_version").HasMaxLength(30);
            entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Hostname).IsUnique();
        });

        // Client Devices
        modelBuilder.Entity<ClientDevice>(entity =>
        {
            entity.ToTable("client_devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.DeviceTokenHash).HasColumnName("device_token_hash").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.RegisteredAt).HasColumnName("registered_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");

            entity.HasOne(e => e.Computer)
                .WithMany(c => c.ClientDevices)
                .HasForeignKey(e => e.ComputerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Printers
        modelBuilder.Entity<Printer>(entity =>
        {
            entity.ToTable("printers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(150);
            entity.Property(e => e.Model).HasColumnName("model").HasMaxLength(150);
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(150);
            entity.Property(e => e.PrinterServer).HasColumnName("printer_server").HasMaxLength(150);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // Print Queues
        modelBuilder.Entity<PrintQueue>(entity =>
        {
            entity.ToTable("print_queues");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PrinterId).HasColumnName("printer_id");
            entity.Property(e => e.QueueName).HasColumnName("queue_name").IsRequired().HasMaxLength(150);
            entity.Property(e => e.QueueType).HasColumnName("queue_type").IsRequired().HasMaxLength(30);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.QueueName).IsUnique();

            entity.HasOne(e => e.Printer)
                .WithMany(p => p.Queues)
                .HasForeignKey(e => e.PrinterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User Sessions
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Computer)
                .WithMany()
                .HasForeignKey(e => e.ComputerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Print Jobs
        modelBuilder.Entity<PrintJob>(entity =>
        {
            entity.ToTable("print_jobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ServerJobId).HasColumnName("server_job_id").HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.PrinterId).HasColumnName("printer_id");
            entity.Property(e => e.DocumentName).HasColumnName("document_name").HasMaxLength(255);
            entity.Property(e => e.TotalPages).HasColumnName("total_pages");
            entity.Property(e => e.Copies).HasColumnName("copies");
            entity.Property(e => e.ColorMode).HasColumnName("color_mode").HasMaxLength(20);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.AuthorizedAt).HasColumnName("authorized_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");

            entity.HasOne(e => e.User)
                .WithMany(u => u.PrintJobs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Computer)
                .WithMany(c => c.PrintJobs)
                .HasForeignKey(e => e.ComputerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Printer)
                .WithMany(p => p.PrintJobs)
                .HasForeignKey(e => e.PrinterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Print Authorizations
        modelBuilder.Entity<PrintAuthorization>(entity =>
        {
            entity.ToTable("print_authorizations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PrintJobId).HasColumnName("print_job_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(30);
            entity.Property(e => e.AuthMethod).HasColumnName("auth_method").HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.PrintJob)
                .WithMany(j => j.Authorizations)
                .HasForeignKey(e => e.PrintJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Computer)
                .WithMany()
                .HasForeignKey(e => e.ComputerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Audit Logs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(80);
            entity.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // System Settings
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SettingKey).HasColumnName("setting_key").IsRequired().HasMaxLength(120);
            entity.Property(e => e.SettingValue).HasColumnName("setting_value").HasColumnType("jsonb");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.SettingKey).IsUnique();
        });

        // Print Job Events
        modelBuilder.Entity<PrintJobEvent>(entity =>
        {
            entity.ToTable("print_job_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PrintJobId).HasColumnName("print_job_id");
            entity.Property(e => e.PreviousStatus).HasColumnName("previous_status").HasMaxLength(30);
            entity.Property(e => e.NewStatus).HasColumnName("new_status").IsRequired().HasMaxLength(30);
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.PrintJob)
                .WithMany(j => j.Events)
                .HasForeignKey(e => e.PrintJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Printer Status Logs
        modelBuilder.Entity<PrinterStatusLog>(entity =>
        {
            entity.ToTable("printer_status_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PrinterId).HasColumnName("printer_id");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(30);
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Printer)
                .WithMany()
                .HasForeignKey(e => e.PrinterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
