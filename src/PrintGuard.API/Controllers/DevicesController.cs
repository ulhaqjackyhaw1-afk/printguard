using Microsoft.AspNetCore.Mvc;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;

namespace PrintGuard.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Hostname))
            return BadRequest(ApiResponse<RegisterDeviceResponse>.Fail("Hostname is required"));

        var result = await _deviceService.RegisterDeviceAsync(request);
        return Ok(result);
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        var result = await _deviceService.HeartbeatAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig([FromQuery] long computerId, [FromHeader(Name = "X-Device-Token")] string? deviceToken)
    {
        if (computerId <= 0)
            return BadRequest(ApiResponse<DeviceConfigResponse>.Fail("Valid computerId is required"));

        var result = await _deviceService.GetDeviceConfigAsync(computerId, deviceToken ?? string.Empty);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] long computerId)
    {
        var result = await _deviceService.RevokeDeviceAsync(computerId);
        return Ok(result);
    }
}
