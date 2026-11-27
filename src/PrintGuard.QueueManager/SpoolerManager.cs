using System.Runtime.InteropServices;
using PrintGuard.QueueManager.Native;

namespace PrintGuard.QueueManager;

public record SpoolerJobDetail(
    int JobId,
    string PrinterName,
    string MachineName,
    string UserName,
    string DocumentName,
    int TotalPages,
    int PagesPrinted,
    int StatusFlags,
    DateTime SubmittedTime
);

public class SpoolerManager
{
    public List<string> GetInstalledPrinterNames()
    {
        var printers = new List<string>();
        int flags = WinSpool.PRINTER_ENUM_LOCAL | WinSpool.PRINTER_ENUM_CONNECTIONS;
        WinSpool.EnumPrinters(flags, null, 2, IntPtr.Zero, 0, out int bytesNeeded, out _);
        if (bytesNeeded == 0) return printers;

        var pBuf = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            if (WinSpool.EnumPrinters(flags, null, 2, pBuf, bytesNeeded, out _, out int count))
            {
                var structSize = Marshal.SizeOf(typeof(WinSpool.PRINTER_INFO_2));
                for (int i = 0; i < count; i++)
                {
                    var ptr = new IntPtr(pBuf.ToInt64() + (i * structSize));
                    var info = Marshal.PtrToStructure<WinSpool.PRINTER_INFO_2>(ptr);
                    if (!string.IsNullOrWhiteSpace(info.pPrinterName))
                    {
                        printers.Add(info.pPrinterName);
                    }
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pBuf);
        }
        return printers;
    }

    public List<SpoolerJobDetail> GetJobs(string printerName)
    {
        var result = new List<SpoolerJobDetail>();
        var def = new WinSpool.PRINTER_DEFAULTS { DesiredAccess = WinSpool.PRINTER_ALL_ACCESS };

        if (!WinSpool.OpenPrinter(printerName, out var hPrinter, ref def))
        {
            if (!WinSpool.OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                return result;
            }
        }

        try
        {
            WinSpool.EnumJobs(hPrinter, 0, 100, 2, IntPtr.Zero, 0, out int bytesNeeded, out int count);
            if (bytesNeeded == 0) return result;

            var pBuf = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (WinSpool.EnumJobs(hPrinter, 0, 100, 2, pBuf, bytesNeeded, out _, out count))
                {
                    var structSize = Marshal.SizeOf(typeof(WinSpool.JOB_INFO_2));
                    for (int i = 0; i < count; i++)
                    {
                        var jobPtr = new IntPtr(pBuf.ToInt64() + (i * structSize));
                        var job = Marshal.PtrToStructure<WinSpool.JOB_INFO_2>(jobPtr);

                        var dt = new DateTime(
                            job.Submitted.wYear,
                            job.Submitted.wMonth,
                            job.Submitted.wDay,
                            job.Submitted.wHour,
                            job.Submitted.wMinute,
                            job.Submitted.wSecond,
                            DateTimeKind.Local
                        );

                        result.Add(new SpoolerJobDetail(
                            job.JobId,
                            job.pPrinterName ?? printerName,
                            job.pMachineName ?? string.Empty,
                            job.pUserName ?? string.Empty,
                            job.pDocument ?? "Untitled Document",
                            job.TotalPages,
                            job.PagesPrinted,
                            job.Status,
                            dt
                        ));
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuf);
            }
        }
        finally
        {
            WinSpool.ClosePrinter(hPrinter);
        }

        return result;
    }

    public bool HoldJob(string printerName, int jobId)
    {
        return SendCommand(printerName, jobId, WinSpool.JOB_CONTROL_PAUSE);
    }

    public bool ReleaseJob(string printerName, int jobId)
    {
        return SendCommand(printerName, jobId, WinSpool.JOB_CONTROL_RESUME);
    }

    public bool CancelJob(string printerName, int jobId)
    {
        return SendCommand(printerName, jobId, WinSpool.JOB_CONTROL_CANCEL) ||
               SendCommand(printerName, jobId, WinSpool.JOB_CONTROL_DELETE);
    }

    private bool SendCommand(string printerName, int jobId, int command)
    {
        var def = new WinSpool.PRINTER_DEFAULTS { DesiredAccess = WinSpool.PRINTER_ALL_ACCESS };
        if (!WinSpool.OpenPrinter(printerName, out var hPrinter, ref def))
        {
            return false;
        }

        try
        {
            return WinSpool.SetJob(hPrinter, jobId, 0, IntPtr.Zero, command);
        }
        finally
        {
            WinSpool.ClosePrinter(hPrinter);
        }
    }
}
