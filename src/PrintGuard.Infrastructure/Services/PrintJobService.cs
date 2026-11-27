using Microsoft.EntityFrameworkCore;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;
using PrintGuard.Domain.Entities;
using PrintGuard.Domain.Enums;
using PrintGuard.Infrastructure.Data;

namespace PrintGuard.Infrastructure.Services;

public class PrintJobService : IPrintJobService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;

    public PrintJobService(ApplicationDbContext db, IPasswordHasher hasher, IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<ApiResponse<PrintJobResponse>> RegisterPrintJobAsync(CreatePrintJobRequest request)
    {
        Printer? printer = null;
        if (request.PrinterId > 0)
        {
            printer = await _db.Printers.FindAsync(request.PrinterId);
        }

        if (printer == null && !string.IsNullOrEmpty(request.PrinterName))
        {
            printer = await _db.Printers.FirstOrDefaultAsync(p => p.Name.ToLower() == request.PrinterName.ToLower());
        }

        if (printer == null)
        {
            // Auto register printer if not yet in database
            printer = new Printer
            {
                Name = !string.IsNullOrEmpty(request.PrinterName) ? request.PrinterName : $"Printer-{request.PrinterId}",
                Model = "Auto-detected Printer",
                Location = "Local PC",
                PrinterServer = Environment.MachineName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Printers.Add(printer);
            await _db.SaveChangesAsync();
        }

        var computer = await _db.Computers.FindAsync(request.ComputerId);
        if (computer == null)
        {
            // Fallback computer
            computer = await _db.Computers.FirstOrDefaultAsync();
        }

        var computerId = computer?.Id ?? request.ComputerId;

        var job = new PrintJob
        {
            ServerJobId = request.ServerJobId,
            ComputerId = computerId > 0 ? computerId : null,
            PrinterId = printer.Id,
            DocumentName = request.DocumentName,
            TotalPages = request.TotalPages > 0 ? request.TotalPages : 1,
            Copies = request.Copies > 0 ? request.Copies : 1,
            ColorMode = request.ColorMode.ToUpperInvariant() == "COLOR" ? "COLOR" : "MONO",
            Status = PrintJobStatus.HELD.ToString(), // Enter HOLD_QUEUE immediately
            SubmittedAt = DateTime.UtcNow
        };

        _db.PrintJobs.Add(job);

        // Record event
        _db.PrintJobEvents.Add(new PrintJobEvent
        {
            PrintJobId = job.Id,
            PreviousStatus = PrintJobStatus.RECEIVED.ToString(),
            NewStatus = PrintJobStatus.HELD.ToString(),
            Details = $"Job received and held waiting for user authentication. Printer: {printer.Name}"
        });

        await _db.SaveChangesAsync();

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, printer.Name, computer.Hostname), "Print job registered and held");
    }

    public async Task<ApiResponse<PrintJobResponse>> GetPrintJobByIdAsync(Guid id)
    {
        var job = await _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .Include(j => j.User)
            .ThenInclude(u => u!.Department)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return ApiResponse<PrintJobResponse>.Fail("Print job not found");

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, job.User?.FullName, job.User?.Department?.Name));
    }

    public async Task<ApiResponse<PrintJobResponse>> AuthorizePrintJobAsync(Guid jobId, AuthorizePrintJobRequest request)
    {
        var job = await _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return ApiResponse<PrintJobResponse>.Fail("Print job not found");

        if (job.Status != PrintJobStatus.HELD.ToString() && job.Status != PrintJobStatus.AUTHENTICATING.ToString())
        {
            return ApiResponse<PrintJobResponse>.Fail($"Cannot authorize job in status: {job.Status}");
        }

        User? user = null;
        string authMethod = AuthMethod.PASSWORD.ToString();

        // 1. Check Remember Me Session Token if provided
        if (!string.IsNullOrEmpty(request.SessionToken))
        {
            var sessions = await _db.UserSessions
                .Include(s => s.User)
                .Where(s => s.ComputerId == request.ComputerId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var s in sessions)
            {
                if (BCrypt.Net.BCrypt.EnhancedVerify(request.SessionToken, s.TokenHash))
                {
                    user = s.User;
                    authMethod = AuthMethod.REMEMBER_ME.ToString();
                    break;
                }
            }
        }

        // 2. If no valid session token, check username & password
        if (user == null && !string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Password))
        {
            user = await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Record denied authorization
                _db.PrintAuthorizations.Add(new PrintAuthorization
                {
                    PrintJobId = job.Id,
                    UserId = user?.Id ?? 0,
                    ComputerId = request.ComputerId,
                    Result = AuthResult.DENIED.ToString(),
                    AuthMethod = AuthMethod.PASSWORD.ToString()
                });

                job.Status = PrintJobStatus.DENIED.ToString();
                job.ErrorMessage = "Authentication failed: invalid username or password";
                await _db.SaveChangesAsync();

                return ApiResponse<PrintJobResponse>.Fail("Authentication failed. Invalid username or password.");
            }
        }

        if (user == null)
        {
            return ApiResponse<PrintJobResponse>.Fail("Missing credentials or valid session token");
        }

        if (!user.IsActive)
        {
            job.Status = PrintJobStatus.DENIED.ToString();
            job.ErrorMessage = "User account is disabled. Printing denied.";
            await _db.SaveChangesAsync();

            return ApiResponse<PrintJobResponse>.Fail("Employee account is disabled. Printing authorization denied.");
        }

        // Create new session if remember me requested with password
        if (request.RememberMe && authMethod == AuthMethod.PASSWORD.ToString())
        {
            var newSessionToken = Guid.NewGuid().ToString("N");
            _db.UserSessions.Add(new UserSession
            {
                UserId = user.Id,
                ComputerId = request.ComputerId,
                TokenHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newSessionToken, 10),
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            });
        }

        // Successful authorization
        var prevStatus = job.Status;
        job.UserId = user.Id;
        job.Status = PrintJobStatus.AUTHORIZED.ToString();
        job.AuthorizedAt = DateTime.UtcNow;

        _db.PrintAuthorizations.Add(new PrintAuthorization
        {
            PrintJobId = job.Id,
            UserId = user.Id,
            ComputerId = request.ComputerId,
            Result = AuthResult.SUCCESS.ToString(),
            AuthMethod = authMethod
        });

        _db.PrintJobEvents.Add(new PrintJobEvent
        {
            PrintJobId = job.Id,
            PreviousStatus = prevStatus,
            NewStatus = PrintJobStatus.AUTHORIZED.ToString(),
            Details = $"Authorized by {user.FullName} ({user.Username}) via {authMethod}"
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(null, user.Id, "PRINT_JOB_AUTHORIZED", "PrintJob", job.Id.ToString(), $"Job {job.Id} for document '{job.DocumentName}' authorized");

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, user.FullName, user.Department?.Name), "Print job authorized successfully");
    }

    public async Task<ApiResponse<PrintJobResponse>> ReleasePrintJobAsync(Guid jobId)
    {
        var job = await _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .Include(j => j.User)
            .ThenInclude(u => u!.Department)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return ApiResponse<PrintJobResponse>.Fail("Print job not found");

        // Idempotency check
        if (job.Status == PrintJobStatus.RELEASING.ToString() ||
            job.Status == PrintJobStatus.PRINTING.ToString() ||
            job.Status == PrintJobStatus.COMPLETED.ToString())
        {
            return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, job.User?.FullName, job.User?.Department?.Name), "Job is already being released or completed");
        }

        if (job.Status != PrintJobStatus.AUTHORIZED.ToString())
        {
            return ApiResponse<PrintJobResponse>.Fail($"Cannot release job that is not AUTHORIZED (current status: {job.Status})");
        }

        var prev = job.Status;
        job.Status = PrintJobStatus.RELEASING.ToString();

        _db.PrintJobEvents.Add(new PrintJobEvent
        {
            PrintJobId = job.Id,
            PreviousStatus = prev,
            NewStatus = PrintJobStatus.RELEASING.ToString(),
            Details = $"Job moved to RELEASE_QUEUE for printer {job.Printer.Name}"
        });

        await _db.SaveChangesAsync();

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, job.User?.FullName, job.User?.Department?.Name), "Job moved to release queue");
    }

    public async Task<ApiResponse<PrintJobResponse>> CancelPrintJobAsync(Guid jobId, string? reason)
    {
        var job = await _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return ApiResponse<PrintJobResponse>.Fail("Print job not found");

        var prev = job.Status;
        job.Status = PrintJobStatus.CANCELLED.ToString();
        job.ErrorMessage = reason ?? "Cancelled by user";

        _db.PrintJobEvents.Add(new PrintJobEvent
        {
            PrintJobId = job.Id,
            PreviousStatus = prev,
            NewStatus = PrintJobStatus.CANCELLED.ToString(),
            Details = $"Job cancelled: {job.ErrorMessage}"
        });

        await _db.SaveChangesAsync();

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, job.User?.FullName), "Job cancelled");
    }

    public async Task<ApiResponse<PrintJobResponse>> CompletePrintJobAsync(Guid jobId)
    {
        var job = await _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return ApiResponse<PrintJobResponse>.Fail("Print job not found");

        var prev = job.Status;
        job.Status = PrintJobStatus.COMPLETED.ToString();
        job.CompletedAt = DateTime.UtcNow;

        _db.PrintJobEvents.Add(new PrintJobEvent
        {
            PrintJobId = job.Id,
            PreviousStatus = prev,
            NewStatus = PrintJobStatus.COMPLETED.ToString(),
            Details = "Job printing completed by spooler"
        });

        await _db.SaveChangesAsync();

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, job.User?.FullName), "Print job completed");
    }

    public async Task<ApiResponse<PrintJobResponse>> FailPrintJobAsync(Guid jobId, string errorMessage)
    {
        var job = await _db.PrintJobs
            .Include(j => j.Printer)
            .Include(j => j.Computer)
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return ApiResponse<PrintJobResponse>.Fail("Print job not found");

        var prev = job.Status;
        job.Status = PrintJobStatus.FAILED.ToString();
        job.ErrorMessage = errorMessage;

        _db.PrintJobEvents.Add(new PrintJobEvent
        {
            PrintJobId = job.Id,
            PreviousStatus = prev,
            NewStatus = PrintJobStatus.FAILED.ToString(),
            Details = $"Job failed: {errorMessage}"
        });

        await _db.SaveChangesAsync();

        return ApiResponse<PrintJobResponse>.Ok(MapToResponse(job, job.Printer.Name, job.Computer?.Hostname, job.User?.FullName), "Print job marked as failed");
    }

    private static PrintJobResponse MapToResponse(PrintJob j, string printerName, string? hostname = null, string? fullName = null, string? deptName = null)
    {
        return new PrintJobResponse(
            j.Id,
            j.ServerJobId,
            j.UserId,
            fullName ?? j.User?.FullName,
            deptName ?? j.User?.Department?.Name,
            j.ComputerId,
            hostname ?? j.Computer?.Hostname,
            j.PrinterId,
            printerName,
            j.DocumentName,
            j.TotalPages,
            j.Copies,
            j.ColorMode,
            j.Status,
            j.SubmittedAt,
            j.AuthorizedAt,
            j.CompletedAt,
            j.ErrorMessage
        );
    }
}
