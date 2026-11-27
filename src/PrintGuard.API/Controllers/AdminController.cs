using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;

namespace PrintGuard.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAdminService _adminService;

    public AdminController(IAuthService authService, IAdminService adminService)
    {
        _authService = authService;
        _adminService = adminService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
    {
        var result = await _authService.AdminLoginAsync(request);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _adminService.GetDashboardSummaryAsync();
        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] long? departmentId,
        [FromQuery] bool? isActive)
    {
        var result = await _adminService.GetUsersAsync(search, departmentId, isActive);
        return Ok(result);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
    {
        var adminId = GetAdminId();
        var result = await _adminService.CreateUserAsync(request, adminId);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    [HttpPut("users/{id:long}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UserUpdateRequest request)
    {
        var adminId = GetAdminId();
        var result = await _adminService.UpdateUserAsync(id, request, adminId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPatch("users/{id:long}/status")]
    public async Task<IActionResult> SetUserStatus(long id, [FromBody] bool isActive)
    {
        var adminId = GetAdminId();
        var result = await _adminService.SetUserStatusAsync(id, isActive, adminId);
        return Ok(result);
    }

    [HttpPost("users/{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] string newPassword)
    {
        var adminId = GetAdminId();
        var result = await _adminService.ResetUserPasswordAsync(id, newPassword, adminId);
        return Ok(result);
    }

    [HttpGet("printers")]
    public async Task<IActionResult> GetPrinters()
    {
        var result = await _adminService.GetPrintersAsync();
        return Ok(result);
    }

    [HttpPost("printers/scan")]
    public async Task<IActionResult> ScanLocalPrinters([FromServices] PrintGuard.Infrastructure.Data.ApplicationDbContext db)
    {
        await PrintGuard.Infrastructure.Services.WindowsPrinterDiscovery.SyncWithDatabaseAsync(db);
        var result = await _adminService.GetPrintersAsync();
        return Ok(result);
    }

    [HttpPost("printers")]
    public async Task<IActionResult> CreatePrinter([FromBody] PrinterDto dto)
    {
        var adminId = GetAdminId();
        var result = await _adminService.CreatePrinterAsync(dto, adminId);
        return StatusCode(201, result);
    }

    [HttpPut("printers/{id:long}")]
    public async Task<IActionResult> UpdatePrinter(long id, [FromBody] PrinterDto dto)
    {
        var adminId = GetAdminId();
        var result = await _adminService.UpdatePrinterAsync(id, dto, adminId);
        return Ok(result);
    }

    [HttpGet("computers")]
    public async Task<IActionResult> GetComputers()
    {
        var result = await _adminService.GetComputersAsync();
        return Ok(result);
    }

    [HttpGet("print-jobs")]
    public async Task<IActionResult> GetPrintJobs(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? userId,
        [FromQuery] long? printerId,
        [FromQuery] string? status)
    {
        var result = await _adminService.GetPrintHistoryAsync(from, to, userId, printerId, status);
        return Ok(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var result = await _adminService.GetAuditLogsAsync();
        return Ok(result);
    }

    private long GetAdminId()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(val, out var id) ? id : 1;
    }
}
