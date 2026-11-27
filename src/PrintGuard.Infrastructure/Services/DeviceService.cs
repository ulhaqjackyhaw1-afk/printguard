using Microsoft.EntityFrameworkCore;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;
using PrintGuard.Infrastructure.Data;

namespace PrintGuard.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditService _audit;

    public DeviceService(ApplicationDbContext db, IJwtTokenService jwt, IAuditService audit)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<ApiResponse<RegisterDeviceResponse>> RegisterDeviceAsync(RegisterDeviceRequest request)
    {
        var computer = await _db.Computers.FirstOrDefaultAsync(c => c.Hostname.ToLower() == request.Hostname.ToLower());
        if (computer == null)
        {
            computer = new Computer
            {
                Hostname = request.Hostname,
                IpAddress = request.IpAddress,
                WindowsVersion = request.WindowsVersion,
                AgentVersion = request.AgentVersion,
                Status = "ONLINE",
                LastSeenAt = DateTime.UtcNow
            };
            _db.Computers.Add(computer);
            await _db.SaveChangesAsync();
        }
        else
        {
            computer.IpAddress = request.IpAddress ?? computer.IpAddress;
            computer.WindowsVersion = request.WindowsVersion ?? computer.WindowsVersion;
            computer.AgentVersion = request.AgentVersion ?? computer.AgentVersion;
            computer.Status = "ONLINE";
            computer.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var rawToken = _jwt.GenerateDeviceToken(computer.Id, computer.Hostname);
        var tokenHash = BCrypt.Net.BCrypt.EnhancedHashPassword(rawToken, 10);

        var device = new ClientDevice
        {
            ComputerId = computer.Id,
            DeviceTokenHash = tokenHash,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        };

        _db.ClientDevices.Add(device);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(null, null, "DEVICE_REGISTERED", "Computer", computer.Id.ToString(), $"Registered host {computer.Hostname}");

        var res = new RegisterDeviceResponse(computer.Id, computer.Hostname, rawToken, computer.Status);
        return ApiResponse<RegisterDeviceResponse>.Ok(res, "Device registered successfully");
    }

    public async Task<ApiResponse<bool>> HeartbeatAsync(HeartbeatRequest request)
    {
        var computer = await _db.Computers.FindAsync(request.ComputerId);
        if (computer == null)
            return ApiResponse<bool>.Fail("Device not registered");

        computer.LastSeenAt = DateTime.UtcNow;
        computer.Status = "ONLINE";
        if (!string.IsNullOrEmpty(request.AgentVersion))
            computer.AgentVersion = request.AgentVersion;
        if (!string.IsNullOrEmpty(request.IpAddress))
            computer.IpAddress = request.IpAddress;

        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Heartbeat acknowledged");
    }

    public async Task<ApiResponse<DeviceConfigResponse>> GetDeviceConfigAsync(long computerId, string deviceToken)
    {
        var computer = await _db.Computers.FindAsync(computerId);
        if (computer == null)
            return ApiResponse<DeviceConfigResponse>.Fail("Invalid computer");

        var printers = await _db.Printers
            .Where(p => p.IsActive)
            .Select(p => new PrinterDto(p.Id, p.Name, p.Model, p.IpAddress, p.Location, p.PrinterServer, p.IsActive, 0, 0))
            .ToListAsync();

        var config = new DeviceConfigResponse(
            RememberMeDurationHours: 8,
            RequireAuthEveryJob: true,
            HeartbeatIntervalSeconds: 60,
            AvailablePrinters: printers
        );

        return ApiResponse<DeviceConfigResponse>.Ok(config);
    }

    public async Task<ApiResponse<bool>> RevokeDeviceAsync(long computerId)
    {
        var devices = await _db.ClientDevices.Where(d => d.ComputerId == computerId && d.IsActive).ToListAsync();
        foreach (var d in devices)
        {
            d.IsActive = false;
            d.RevokedAt = DateTime.UtcNow;
        }

        var computer = await _db.Computers.FindAsync(computerId);
        if (computer != null)
        {
            computer.Status = "REVOKED";
        }

        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Device credentials revoked");
    }
}
