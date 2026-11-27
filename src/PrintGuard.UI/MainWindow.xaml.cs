using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using PrintGuard.Application.DTOs;

namespace PrintGuard.UI;

public partial class MainWindow : Window
{
    private readonly string _jobId;
    private readonly string _printerName;
    private readonly string _documentName;
    private readonly long _computerId;
    private readonly int _spoolerJobId;
    private readonly HttpClient _http;

    private static readonly string SessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PrintGuard",
        "session.dat"
    );

    public MainWindow(string jobId, string printerName, string documentName, long computerId, int spoolerJobId)
    {
        InitializeComponent();

        _jobId = jobId;
        _printerName = printerName;
        _documentName = documentName;
        _computerId = computerId;
        _spoolerJobId = spoolerJobId;

        TxtDocName.Text = _documentName;
        TxtPrinterName.Text = _printerName;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7148/api/v1/")
        };

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Check for existing Remember Me session token
        var savedToken = LoadSavedSession();
        if (!string.IsNullOrEmpty(savedToken) && Guid.TryParse(_jobId, out var parsedGuid))
        {
            // Attempt automatic authorization with session token
            await AttemptSessionAuthorize(parsedGuid, savedToken);
        }
    }

    private async Task AttemptSessionAuthorize(Guid jobId, string sessionToken)
    {
        try
        {
            BtnAuthorize.IsEnabled = false;
            BtnAuthorize.Content = "Authenticating Session...";

            var req = new AuthorizePrintJobRequest(
                Username: null,
                Password: null,
                SessionToken: sessionToken,
                ComputerId: _computerId,
                RememberMe: true
            );

            var res = await _http.PostAsJsonAsync($"print-jobs/{jobId}/authorize", req);
            if (res.IsSuccessStatusCode)
            {
                // Authorized!
                Close();
                return;
            }
        }
        catch
        {
            // Ignore session failure and prompt for manual login
        }
        finally
        {
            BtnAuthorize.IsEnabled = true;
            BtnAuthorize.Content = "Authorize & Print";
        }
    }

    private async void BtnAuthorize_Click(object sender, RoutedEventArgs e)
    {
        BorderError.Visibility = Visibility.Collapsed;
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Please enter both username and password.");
            return;
        }

        BtnAuthorize.IsEnabled = false;
        BtnAuthorize.Content = "Authorizing...";

        try
        {
            if (!Guid.TryParse(_jobId, out var jobIdGuid))
            {
                // In standalone demo mode, validate against login endpoint
                var loginReq = new LoginRequest(username, password, _computerId, ChkRememberMe.IsChecked ?? false);
                var loginRes = await _http.PostAsJsonAsync("auth/login", loginReq);
                if (!loginRes.IsSuccessStatusCode)
                {
                    ShowError("Invalid username or password, or user is disabled.");
                    return;
                }

                var loginData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
                if (loginData?.Data?.SessionToken != null)
                {
                    SaveSession(loginData.Data.SessionToken);
                }

                MessageBox.Show("Print authorization successful!", "PrintGuard", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            var authReq = new AuthorizePrintJobRequest(
                Username: username,
                Password: password,
                SessionToken: null,
                ComputerId: _computerId,
                RememberMe: ChkRememberMe.IsChecked ?? false
            );

            var response = await _http.PostAsJsonAsync($"print-jobs/{jobIdGuid}/authorize", authReq);
            if (response.IsSuccessStatusCode)
            {
                if (ChkRememberMe.IsChecked == true)
                {
                    // Optionally save session token if returned
                }

                Close();
            }
            else
            {
                var errorObj = await response.Content.ReadFromJsonAsync<ApiResponse<PrintJobResponse>>();
                ShowError(errorObj?.Message ?? "Authentication failed.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            BtnAuthorize.IsEnabled = true;
            BtnAuthorize.Content = "Authorize & Print";
        }
    }

    private async void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (Guid.TryParse(_jobId, out var jobIdGuid))
        {
            try
            {
                await _http.PostAsJsonAsync($"print-jobs/{jobIdGuid}/cancel", "Cancelled by user via GUI");
            }
            catch { }
        }

        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Closing GUI keeps job held, does NOT release!
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        BorderError.Visibility = Visibility.Visible;
    }

    private static void SaveSession(string token)
    {
        try
        {
            var dir = Path.GetDirectoryName(SessionFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(SessionFilePath, token);
        }
        catch { }
    }

    private static string? LoadSavedSession()
    {
        try
        {
            if (File.Exists(SessionFilePath))
                return File.ReadAllText(SessionFilePath).Trim();
        }
        catch { }
        return null;
    }
}
