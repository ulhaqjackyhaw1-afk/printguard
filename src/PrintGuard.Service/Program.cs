using PrintGuard.Service;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "PrintGuard Service";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
