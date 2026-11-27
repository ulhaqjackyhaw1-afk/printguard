using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using PrintGuard.Application.DTOs;
using PrintGuard.Domain.Entities;
using PrintGuard.Infrastructure.Data;

namespace PrintGuard.Infrastructure.Services;

public static class WindowsPrinterDiscovery
{
    private const int PRINTER_ENUM_LOCAL = 0x00000002;
    private const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PRINTER_INFO_2
    {
        public string? pServerName;
        public string? pPrinterName;
        public string? pShareName;
        public string? pPortName;
        public string? pDriverName;
        public string? pComment;
        public string? pLocation;
        public IntPtr pDevMode;
        public string? pSepFile;
        public string? pPrintProcessor;
        public string? pDatatype;
        public string? pParameters;
        public IntPtr pSecurityDescriptor;
        public int Attributes;
        public int Priority;
        public int DefaultPriority;
        public int StartTime;
        public int UntilTime;
        public int Status;
        public int cJobs;
        public int AveragePPM;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumPrinters(
        int Flags,
        string? Name,
        int Level,
        IntPtr pPrinterEnum,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    public static List<PrinterDto> DiscoverLocalPrinters()
    {
        var list = new List<PrinterDto>();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return list;
        }

        try
        {
            int flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;
            EnumPrinters(flags, null, 2, IntPtr.Zero, 0, out int bytesNeeded, out _);
            if (bytesNeeded == 0) return list;

            var pBuf = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (EnumPrinters(flags, null, 2, pBuf, bytesNeeded, out _, out int count))
                {
                    var structSize = Marshal.SizeOf(typeof(PRINTER_INFO_2));
                    for (int i = 0; i < count; i++)
                    {
                        var ptr = new IntPtr(pBuf.ToInt64() + (i * structSize));
                        var info = Marshal.PtrToStructure<PRINTER_INFO_2>(ptr);

                        if (!string.IsNullOrWhiteSpace(info.pPrinterName))
                        {
                            list.Add(new PrinterDto(
                                Id: 0,
                                Name: info.pPrinterName,
                                Model: info.pDriverName ?? "Standard Printer",
                                IpAddress: info.pPortName,
                                Location: string.IsNullOrEmpty(info.pLocation) ? Environment.MachineName : info.pLocation,
                                PrinterServer: Environment.MachineName,
                                IsActive: true,
                                TotalJobs: 0,
                                TotalPages: 0
                            ));
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuf);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintGuard] Could not discover Windows printers: {ex.Message}");
        }

        return list;
    }

    public static async Task SyncWithDatabaseAsync(ApplicationDbContext db)
    {
        var detected = DiscoverLocalPrinters();
        if (detected.Count == 0) return;

        foreach (var p in detected)
        {
            var exists = await db.Printers.FirstOrDefaultAsync(x => x.Name.ToLower() == p.Name.ToLower());
            if (exists == null)
            {
                var newPrinter = new Printer
                {
                    Name = p.Name,
                    Model = p.Model,
                    IpAddress = p.IpAddress,
                    Location = p.Location,
                    PrinterServer = p.PrinterServer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.Printers.Add(newPrinter);
                await db.SaveChangesAsync();

                db.PrintQueues.Add(new PrintQueue
                {
                    PrinterId = newPrinter.Id,
                    QueueName = $"{newPrinter.Name}-HOLD",
                    QueueType = "HOLD",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();

                Console.WriteLine($"[PrintGuard] Auto-registered local printer: {newPrinter.Name}");
            }
        }
    }
}
