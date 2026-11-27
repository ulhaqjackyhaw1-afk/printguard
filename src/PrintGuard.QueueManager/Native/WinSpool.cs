using System.Runtime.InteropServices;

namespace PrintGuard.QueueManager.Native;

public static class WinSpool
{
    public const int PRINTER_ACCESS_ADMINISTER = 0x00000004;
    public const int PRINTER_ACCESS_USE = 0x00000008;
    public const int PRINTER_ALL_ACCESS = 0x000F000C;

    public const int JOB_CONTROL_PAUSE = 1;
    public const int JOB_CONTROL_RESUME = 2;
    public const int JOB_CONTROL_CANCEL = 3;
    public const int JOB_CONTROL_RESTART = 4;
    public const int JOB_CONTROL_DELETE = 5;
    public const int JOB_CONTROL_SENT_TO_PRINTER = 6;
    public const int JOB_CONTROL_LAST_PAGE_EJECTED = 7;

    public const int JOB_STATUS_PAUSED = 0x00000001;
    public const int JOB_STATUS_ERROR = 0x00000002;
    public const int JOB_STATUS_DELETING = 0x00000004;
    public const int JOB_STATUS_SPOOLING = 0x00000008;
    public const int JOB_STATUS_PRINTING = 0x00000010;
    public const int JOB_STATUS_OFFLINE = 0x00000020;
    public const int JOB_STATUS_PAPEROUT = 0x00000040;
    public const int JOB_STATUS_PRINTED = 0x00000080;
    public const int JOB_STATUS_DELETED = 0x00000100;
    public const int JOB_STATUS_BLOCKED_DEVQ = 0x00000200;
    public const int JOB_STATUS_USER_INTERVENTION = 0x00000400;
    public const int JOB_STATUS_RESTART = 0x00000800;
    public const int JOB_STATUS_COMPLETE = 0x00001000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PRINTER_DEFAULTS
    {
        public IntPtr pDatatype;
        public IntPtr pDevMode;
        public int DesiredAccess;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SYSTEMTIME
    {
        public short wYear;
        public short wMonth;
        public short wDayOfWeek;
        public short wDay;
        public short wHour;
        public short wMinute;
        public short wSecond;
        public short wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct JOB_INFO_2
    {
        public int JobId;
        public string? pPrinterName;
        public string? pMachineName;
        public string? pUserName;
        public string? pDocument;
        public string? pNotifyName;
        public string? pDatatype;
        public string? pPrintProcessor;
        public string? pParameters;
        public string? pDriverName;
        public IntPtr pDevMode;
        public string? pStatus;
        public IntPtr pSecurityDescriptor;
        public int Status;
        public int Priority;
        public int Position;
        public int StartTime;
        public int UntilTime;
        public int TotalPages;
        public int Size;
        public SYSTEMTIME Submitted;
        public int Time;
        public int PagesPrinted;
    }

    public const int PRINTER_ENUM_LOCAL = 0x00000002;
    public const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PRINTER_INFO_2
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
    public static extern bool EnumPrinters(
        int Flags,
        string? Name,
        int Level,
        IntPtr pPrinterEnum,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, ref PRINTER_DEFAULTS pDefault);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool EnumJobs(
        IntPtr hPrinter,
        int FirstJob,
        int NoJobs,
        int Level,
        IntPtr pJob,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool SetJob(IntPtr hPrinter, int JobId, int Level, IntPtr pJob, int Command);
}
