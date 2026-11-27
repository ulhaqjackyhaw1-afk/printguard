using Microsoft.AspNetCore.Mvc;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;

namespace PrintGuard.API.Controllers;

[ApiController]
[Route("api/v1/print-jobs")]
public class PrintJobsController : ControllerBase
{
    private readonly IPrintJobService _jobService;

    public PrintJobsController(IPrintJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterPrintJob([FromBody] CreatePrintJobRequest request)
    {
        var result = await _jobService.RegisterPrintJobAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPrintJob(Guid id)
    {
        var result = await _jobService.GetPrintJobByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/authorize")]
    public async Task<IActionResult> Authorize(Guid id, [FromBody] AuthorizePrintJobRequest request)
    {
        var result = await _jobService.AuthorizePrintJobAsync(id, request);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/release")]
    public async Task<IActionResult> Release(Guid id)
    {
        var result = await _jobService.ReleasePrintJobAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason)
    {
        var result = await _jobService.CancelPrintJobAsync(id, reason);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var result = await _jobService.CompletePrintJobAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/fail")]
    public async Task<IActionResult> Fail(Guid id, [FromBody] string errorMessage)
    {
        var result = await _jobService.FailPrintJobAsync(id, errorMessage);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
