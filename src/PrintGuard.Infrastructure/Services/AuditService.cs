using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;
using PrintGuard.Infrastructure.Data;

namespace PrintGuard.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        long? adminUserId,
        long? userId,
        string action,
        string? entityType,
        string? entityId,
        string? description,
        string? ipAddress = null)
    {
        var log = new AuditLog
        {
            AdminUserId = adminUserId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
