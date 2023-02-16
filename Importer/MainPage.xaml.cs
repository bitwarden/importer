using CsvHelper;
using PasswordManagerAccess.LastPass;
using System.Diagnostics;
using System.Globalization;

namespace Bit.Importer;

public partial class MainPage : ContentPage
{
    private readonly List<string> _services = new() { "LastPass" };
    private string _bitwardenCloudUrl = "https://bitwarden.com";

    public MainPage()
    {
        InitializeComponent();

        BitwardenServerUrl.Text = _bitwardenCloudUrl;
        Service.ItemsSource = _services;
        Service.SelectedIndex = 0;

        var learnMoreTap = new TapGestureRecognizer();
        learnMoreTap.Tapped += async (s, e) =>
        {
            await Browser.Default.OpenAsync(new Uri("https://bitwarden.com/help"),
                BrowserLaunchMode.SystemPreferred);
        };
        LearnMore.GestureRecognizers.Add(learnMoreTap);

        var apiKeyTap = new TapGestureRecognizer();
        apiKeyTap.Tapped += async (s, e) =>
        {
            await Browser.Default.OpenAsync(new Uri("https://vault.bitwarden.com/#/settings/security/security-keys"),
                BrowserLaunchMode.SystemPreferred);
        };
        ApiKeyLink1.GestureRecognizers.Add(apiKeyTap);
        ApiKeyLink2.GestureRecognizers.Add(apiKeyTap);
    }

    private void BitwardenKeyConnector_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        // We don't need master passwqord if using key connector
        BitwardenPasswordLayout.IsVisible = !BitwardenKeyConnector.IsChecked;
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        // Validate
        if (!await ValidateInputsAsync())
        {
            return;
        }

        // Start loading state
        Loading.IsRunning = true;
        SubmitButton.IsEnabled = false;
        PleaseWait.IsVisible = true;

        // Run the import task
        await Task.Run(ImportAsync);
    }

    private async Task<bool> ValidateInputsAsync()
    {
        if (string.IsNullOrWhiteSpace(BitwardenApiKeyClientId?.Text) ||
            string.IsNullOrWhiteSpace(BitwardenApiKeySecret?.Text))
        {
            await DisplayAlert("Error", "Bitwarden API Key information is required.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(BitwardenPassword?.Text) &&
            !BitwardenKeyConnector.IsChecked)
        {
            await DisplayAlert("Error", "Bitwarden master password is required.", "OK");
            return false;
        }

        if (_services[Service.SelectedIndex] == "LastPass")
        {
            if (string.IsNullOrWhiteSpace(LastPassEmail?.Text) ||
                string.IsNullOrWhiteSpace(LastPassPassword?.Text))
            {
                await DisplayAlert("Error", "LastPass Email and Master Password are required.", "OK");
                return false;
            }
        }

        return true;
    }

    private async Task ImportAsync()
    {
        await CleanupAsync();

        if (_services[Service.SelectedIndex] == "LastPass")
        {
            var (lastpassSuccess, lastpassCsvPath) = await CreateLastpassCsvAsync();
            if (!lastpassSuccess)
            {
                StopLoadingAndAlert(true,
                    "Unable to log into your LastPass account. Are your credentials correct?");
            }

            var cliSetupSuccess = false;
            try
            {
                if (lastpassSuccess)
                {
                    await SetupCliAsync();
                    cliSetupSuccess = true;
                }
            }
            catch
            {
                StopLoadingAndAlert(true, "Unable to set up Bitwarden CLI.");
            }

            if (cliSetupSuccess && lastpassSuccess)
            {
                if (!string.IsNullOrWhiteSpace(BitwardenServerUrl?.Text) && BitwardenServerUrl.Text != "https://bitwarden.com")
                {
                    var configServerSuccess = ConfigServerCli();
                    if (!configServerSuccess)
                    {
                        StopLoadingAndAlert(true, "Unable to configure Bitwarden server.");
                        return;
                    }
                }

                var (loginSuccess, sessionKey) = LogInCli();
                if (!loginSuccess)
                {
                    StopLoadingAndAlert(true,
                        "Unable to log into your Bitwarden account. Is your API key information correct?");
                }

                if (loginSuccess && string.IsNullOrWhiteSpace(sessionKey))
                {
                    var (unlockSuccess, unlockSessionKey) = UnlockCli();
                    sessionKey = unlockSessionKey;
                    if (!unlockSuccess)
                    {
                        StopLoadingAndAlert(true,
                            "Unable to unlock your Bitwarden vault. Is your master password correct?");
                    }
                }

                if (!string.IsNullOrWhiteSpace(lastpassCsvPath) && !string.IsNullOrWhiteSpace(sessionKey))
                {
                    var importSuccess = ImportCli("lastpasscsv", lastpassCsvPath, sessionKey);
                    if (importSuccess)
                    {
                        StopLoadingAndAlert(false, "Your import was successful!");
                    }
                    else
                    {
                        StopLoadingAndAlert(true, "Something went wrong with the import.");
                    }
                }
            }
        }

        await CleanupAsync();
    }

    private void StopLoadingAndAlert(bool error, string message)
    {
        Dispatcher.Dispatch(async () =>
        {
            // Stop the loading state
            Loading.IsRunning = false;
            SubmitButton.IsEnabled = true;
            PleaseWait.IsVisible = false;

            // Show alert
            if (error)
            {
                await DisplayAlert("Error", message, "OK");
            }
            else
            {
                await DisplayAlert("Success", message, "OK");
            }
        });
    }

    private async Task<(bool, string)> CreateLastpassCsvAsync()
    {
        try
        {
            // Log in and get LastPass data
            var ui = new Services.LastPass.Ui(this);
            var clientInfo = new ClientInfo(
                PasswordManagerAccess.LastPass.Platform.Desktop,
                Guid.NewGuid().ToString().ToLower(),
                "Importer");
            var vault = Vault.Open(LastPassEmail?.Text, LastPassPassword?.Text, clientInfo, ui);

            // Massage it to expected CSV format
            var exportAccounts = vault.Accounts.Select(a => new Services.LastPass.ExportedAccount(a));

            // Create CSV string
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(exportAccounts);
            csv.Flush();
            var csvOutput = writer.ToString();
            // Write CSV to temp disk

            var lastpassCsvPath = Path.Combine(FileSystem.CacheDirectory, "lastpass-export.csv");
            await File.WriteAllTextAsync(lastpassCsvPath, csvOutput);
            return (true, lastpassCsvPath);
        }
        catch
        {
            return (false, null);
        }
    }

    private bool ConfigServerCli()
    {
        var (exitCode, stdOut) = ExecCli($"config server {BitwardenServerUrl?.Text}");
        return exitCode == 0;
    }

    private (bool, string) LogInCli()
    {
        var (exitCode, sessionKey) = ExecCli("login --apikey --raw", (process) =>
        {
            process.StartInfo.EnvironmentVariables["BW_CLIENTID"] = BitwardenApiKeyClientId?.Text;
            process.StartInfo.EnvironmentVariables["BW_CLIENTSECRET"] = BitwardenApiKeySecret?.Text;
            // Avoid BW_NOINTERACTION bug that is issuing invalid session key on api key login
            process.StartInfo.EnvironmentVariables["BW_NOINTERACTION"] = "false";
        });
        return (exitCode == 0, sessionKey);
    }

    private (bool, string) UnlockCli()
    {
        var (exitCode, sessionKey) = ExecCli($"unlock {BitwardenPassword?.Text} --raw");
        return (exitCode == 0, sessionKey);
    }

    private bool ImportCli(string importService, string importFilePath, string sessionKey)
    {
        var (importExitCode, importStdOut) = ExecCli($"import {importService} {importFilePath}", (process) =>
        {
            process.StartInfo.EnvironmentVariables["BW_SESSION"] = sessionKey;
        });
        return importExitCode == 0 && (importStdOut?.Contains("Imported") ?? false);
    }

    private async Task SetupCliAsync()
    {
        // Copy packaged CLI app to disk so that we can invoke it.
        using var stream = await FileSystem.OpenAppPackageFileAsync("bw-cli/bw-windows.exe");
        using var fileStream = File.Create(ResolveCliPath());
        stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(fileStream);
        stream.Close();
        fileStream.Close();
    }

    private (int, string) ExecCli(string args, Action<Process> processAction = null)
    {
        // Set up the process
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ResolveCliPath(),
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            },
        };

        // Load standard env vars for this use case
        process.StartInfo.EnvironmentVariables["BITWARDENCLI_APPDATA_DIR"] = FileSystem.CacheDirectory;
        process.StartInfo.EnvironmentVariables["BW_NOINTERACTION"] = "true";
        processAction?.Invoke(process);

        process.Start();
        var stdOut = "";
        while (!process.StandardOutput.EndOfStream)
        {
            stdOut += process.StandardOutput.ReadLine();
        }
        process.StandardOutput.Close();
        process.WaitForExit();
        return (process.ExitCode, stdOut.Trim());
    }

    private string ResolveCliPath()
    {
        var bwCliFilename = DeviceInfo.Platform == DevicePlatform.WinUI ? "bw.exe" : "bw";
        return Path.Combine(FileSystem.CacheDirectory, bwCliFilename);
    }

    private Task CleanupAsync()
    {
        File.Delete(Path.Combine(FileSystem.CacheDirectory, "data.json"));
        File.Delete(Path.Combine(FileSystem.CacheDirectory, "lastpass-export.csv"));
        return Task.FromResult(0);
    }
}