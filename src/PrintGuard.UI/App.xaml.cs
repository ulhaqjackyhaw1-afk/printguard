using System.Windows;

namespace PrintGuard.UI;

public partial class App : System.Windows.Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        string jobId = string.Empty;
        string printer = "PRN-LAN-01-FINANCE";
        string doc = "Document.pdf";
        long computerId = 1;
        int spoolerId = 101;

        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i] == "--job-id" && i + 1 < e.Args.Length) jobId = e.Args[i + 1];
            if (e.Args[i] == "--printer" && i + 1 < e.Args.Length) printer = e.Args[i + 1];
            if (e.Args[i] == "--document" && i + 1 < e.Args.Length) doc = e.Args[i + 1];
            if (e.Args[i] == "--computer-id" && i + 1 < e.Args.Length && long.TryParse(e.Args[i + 1], out var cid)) computerId = cid;
            if (e.Args[i] == "--spooler-id" && i + 1 < e.Args.Length && int.TryParse(e.Args[i + 1], out var sid)) spoolerId = sid;
        }

        var mainWindow = new MainWindow(jobId, printer, doc, computerId, spoolerId);
        mainWindow.Show();
    }
}
