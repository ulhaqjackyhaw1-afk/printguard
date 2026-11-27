using System.Diagnostics;
using System.Net.Http.Json;
using PrintGuard.Application.DTOs;
using PrintGuard.QueueManager;

namespace PrintGuard.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly SpoolerManager _spooler;

    private long _computerId;
    private string _deviceToken = string.Empty;
    private readonly HashSet<int> _processedJobs = new();
    private readonly Dictionary<int, Guid> _spoolerToDbJobMap = new();

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        var handler = new HttpClientHandler
        {
            // Bypass SSL check for local intranet self-signed certificates if needed
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(_config["PrintGuard:ApiBaseUrl"] ?? "https://localhost:7148/api/v1/")
        };
        _spooler = new SpoolerManager();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrintGuard.Service started at: {time}", DateTimeOffset.Now);

        // 1. Register Client Device
        await RegisterDeviceAsync(stoppingToken);

        var pollInterval = TimeSpan.FromSeconds(_config.GetValue<int>("PrintGuard:QueuePollIntervalSeconds", 2));
        var heartbeatInterval = TimeSpan.FromSeconds(_config.GetValue<int>("PrintGuard:HeartbeatIntervalSeconds", 60));
        var lastHeartbeat = DateTime.MinValue;

        var watchedPrinters = _config.GetSection("PrintGuard:WatchedPrinters").Get<string[]>() ?? Array.Empty<string>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Heartbeat check
                if (DateTime.UtcNow - lastHeartbeat >= heartbeatInterval)
                {
                    await SendHeartbeatAsync(stoppingToken);
                    lastHeartbeat = DateTime.UtcNow;
                }

                // Spooler Queue Monitoring (dynamic over all installed physical/virtual printers)
                var printersToWatch = _config.GetSection("PrintGuard:WatchedPrinters").Get<string[]>() ?? Array.Empty<string>();
                if (printersToWatch.Length == 0)
                {
                    printersToWatch = _spooler.GetInstalledPrinterNames().ToArray();
                }

                foreach (var printer in printersToWatch)
                {
                    var jobs = _spooler.GetJobs(printer);
                    foreach (var job in jobs)
                    {
                        if (!_processedJobs.Contains(job.JobId))
                        {
                            _logger.LogInformation("New print job detected: JobId={JobId}, Printer={Printer}, Document={Doc}, User={User}",
                                job.JobId, printer, job.DocumentName, job.UserName);

                            // HOLD Job immediately (Fail-closed)
                            _spooler.HoldJob(printer, job.JobId);
                            _processedJobs.Add(job.JobId);

                            // Register with Central Server API
                            var dbJobId = await RegisterJobWithServerAsync(printer, job, stoppingToken);
                            if (dbJobId.HasValue)
                            {
                                _spoolerToDbJobMap[job.JobId] = dbJobId.Value;
                                // Launch GUI popup for user
                                TriggerUiPopup(dbJobId.Value, printer, job.DocumentName, job.JobId);
                            }
                        }
                    }
                }

                // Check authorization status for held jobs
                await CheckPendingAuthorizationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in worker loop");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task RegisterDeviceAsync(CancellationToken ct)
    {
        try
        {
            var hostname = Environment.MachineName;
            var request = new RegisterDeviceRequest(
                Hostname: hostname,
                IpAddress: "127.0.0.1",
                WindowsVersion: Environment.OSVersion.ToString(),
                AgentVersion: "1.0.0"
            );

            var res = await _http.PostAsJsonAsync("devices/register", request, ct);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<RegisterDeviceResponse>>(cancellationToken: ct);
                if (body?.Data != null)
                {
                    _computerId = body.Data.ComputerId;
                    _deviceToken = body.Data.DeviceToken;
                    _logger.LogInformation("Device registered successfully with ID {ComputerId}", _computerId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device with central server");
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken ct)
    {
        if (_computerId == 0) return;

        try
        {
            var req = new HeartbeatRequest(_computerId, _deviceToken, "1.0.0", "127.0.0.1");
            await _http.PostAsJsonAsync("devices/heartbeat", req, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    private async Task<Guid?> RegisterJobWithServerAsync(string printerName, SpoolerJobDetail job, CancellationToken ct)
    {
        try
        {
            var req = new CreatePrintJobRequest(
                ServerJobId: $"SPOOL-{job.JobId}",
                ComputerId: _computerId,
                PrinterId: 0,
                DocumentName: job.DocumentName,
                TotalPages: job.TotalPages > 0 ? job.TotalPages : 1,
                Copies: 1,
                ColorMode: "MONO",
                PrinterName: printerName
            );

            var res = await _http.PostAsJsonAsync("print-jobs", req, ct);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PrintJobResponse>>(cancellationToken: ct);
                return body?.Data?.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register print job with backend");
        }
        return null;
    }

    private async Task CheckPendingAuthorizationsAsync(CancellationToken ct)
    {
        var activeJobs = _spoolerToDbJobMap.ToList();
        foreach (var kvp in activeJobs)
        {
            var spoolerJobId = kvp.Key;
            var dbJobId = kvp.Value;

            try
            {
                var res = await _http.GetFromJsonAsync<ApiResponse<PrintJobResponse>>($"print-jobs/{dbJobId}", ct);
                if (res?.Data != null)
                {
                    var status = res.Data.Status;
                    if (status == "AUTHORIZED")
                    {
                        _logger.LogInformation("Job {JobId} AUTHORIZED. Releasing to printer...", spoolerJobId);
                        // Release job in Spooler
                        _spooler.ReleaseJob(res.Data.PrinterName, spoolerJobId);

                        // Call release endpoint on server
                        await _http.PostAsync($"print-jobs/{dbJobId}/release", null, ct);
                        _spoolerToDbJobMap.Remove(spoolerJobId);
                    }
                    else if (status == "DENIED" || status == "CANCELLED")
                    {
                        _logger.LogInformation("Job {JobId} status {Status}. Cancelling job in spooler...", spoolerJobId, status);
                        _spooler.CancelJob(res.Data.PrinterName, spoolerJobId);
                        _spoolerToDbJobMap.Remove(spoolerJobId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check status for job {DbJobId}", dbJobId);
            }
        }
    }

    private void TriggerUiPopup(Guid dbJobId, string printerName, string docName, int spoolerJobId)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var directExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "PrintGuard.UI", "bin", "Debug", "net8.0-windows", "PrintGuard.UI.exe"));
            var localExe = Path.Combine(baseDir, "PrintGuard.UI.exe");
            var projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "PrintGuard.UI"));

            var args = $"--job-id {dbJobId} --printer \"{printerName}\" --document \"{docName}\" --computer-id {_computerId} --spooler-id {spoolerJobId}";
            _logger.LogInformation("Launching UI Popup with args: {Args}", args);

            if (File.Exists(directExe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = directExe,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            else if (File.Exists(localExe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = localExe,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{projectDir}\" -- {args}",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not launch UI popup");
        }
    }
}
