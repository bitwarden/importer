using Bit.Importer.Services;
using Bit.Importer.Services.LastPass;
using Bit.Importer.Services.OnePassword;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Bit.Importer;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient = new();
    private readonly bool _doLogging = false;
    private readonly string _cacheDir;
    private readonly List<string> _services = new() { "LastPass"/*, "1Password"*/ };
    private readonly string _cliVersion = "2023.4.0";
    private readonly string _cliBaseDownloadUrl = "https://assets.bitwarden.com/importer";
    private readonly string _bitwardenCloudUrl = "https://bitwarden.com";
    private readonly Dictionary<string, int> _twoFactorMethods =
        new() { { "Authenticator app", 0 }, { "Email", 1 }, { "YubiKey OTP security key", 3 } };

    public MainPage()
    {
        _cacheDir = Path.Combine(FileSystem.CacheDirectory, "com.bitwarden.importer");

        InitializeComponent();

        BitwardenServerUrl.Text = _bitwardenCloudUrl;
        Service.ItemsSource = _services;
        Service.SelectedIndex = 0;
        Service.IsVisible = ServiceLabel.IsVisible = _services.Count > 1;

        var learnMoreTap = new TapGestureRecognizer();
        learnMoreTap.Tapped += async (s, e) =>
        {
            await Browser.Default.OpenAsync(new Uri("https://bitwarden.com/help/bitwarden-importer-tool/"),
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

        CachePath.Text = _cacheDir;
        ParseCommandlineDefaults();
    }

    private void BitwardenKeyConnector_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        // We don't need master passwqord if using key connector
        BitwardenPasswordLayout.IsVisible = !BitwardenKeyConnector.IsChecked;
    }

    private void BitwardenApiKeyOption_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        BitwardenApiKeyLayout.IsVisible = BitwardenApiKeyOption.IsChecked;
        BitwardenEmailLayout.IsVisible = !BitwardenApiKeyOption.IsChecked;
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

    private void Service_SelectedIndexChanged(object sender, EventArgs e)
    {
        LastPassLayout.IsVisible = _services[Service.SelectedIndex] == "LastPass";
        OnePasswordLayout.IsVisible = _services[Service.SelectedIndex] == "1Password";
    }

    private async Task<bool> ValidateInputsAsync()
    {
        if (BitwardenApiKeyOption.IsChecked &&
            (string.IsNullOrWhiteSpace(BitwardenApiKeyClientId?.Text) ||
            string.IsNullOrWhiteSpace(BitwardenApiKeySecret?.Text)))
        {
            await DisplayAlert("Error", "Bitwarden API key information is required.", "OK");
            return false;
        }

        if (!BitwardenApiKeyOption.IsChecked &&
            string.IsNullOrWhiteSpace(BitwardenEmail?.Text))
        {
            await DisplayAlert("Error", "Bitwarden email is required.", "OK");
            return false;
        }

        if (BitwardenKeyConnector.IsChecked && !BitwardenApiKeyOption.IsChecked)
        {
            await DisplayAlert("Error", "Bitwarden APIs keys are required when your organization uses SSO.", "OK");
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

        if (_services[Service.SelectedIndex] == "1Password")
        {
            if (string.IsNullOrWhiteSpace(OnePasswordEmail?.Text) ||
                string.IsNullOrWhiteSpace(OnePasswordSecretKey?.Text) ||
                string.IsNullOrWhiteSpace(OnePasswordDomain?.Text) ||
                string.IsNullOrWhiteSpace(OnePasswordPassword?.Text))
            {
                await DisplayAlert("Error", "1Password information is required.", "OK");
                return false;
            }
        }

        return true;
    }

    private async Task ImportAsync()
    {
        await SetupAsync();
        await CleanupAsync();

        IImportService importService = null;
        var serviceSelection = _services[Service.SelectedIndex];
        if (serviceSelection == "LastPass")
        {
            importService = new LastPassImportService(this, _cacheDir,
                LastPassEmail?.Text, LastPassPassword?.Text, LastPassSkipShared.IsChecked);
        }
        else if (serviceSelection == "1Password")
        {
            importService = new OnePasswordImportService(this, _cacheDir,
                OnePasswordEmail?.Text, OnePasswordSecretKey?.Text, OnePasswordPassword?.Text,
                OnePasswordDomain?.Text, OnePasswordExcludedVaults.Text);
        }

        var (serviceSuccess, importFilePath, importOption) = (false, string.Empty, string.Empty);
        if (importService != null)
        {
            (serviceSuccess, importFilePath, importOption) = await importService.CreateImportFileAsync();
        }

        if (!serviceSuccess)
        {
            StopLoadingAndAlert(true,
                $"Unable to log into your {serviceSelection} account. Are your credentials correct?");
        }

        var cliSetupSuccess = false;
        try
        {
            if (serviceSuccess)
            {
                await SetupCliAsync();
                cliSetupSuccess = true;
            }
        }
        catch
        {
            StopLoadingAndAlert(true, "Unable to set up Bitwarden CLI.");
        }

        if (cliSetupSuccess && serviceSuccess)
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
                if (BitwardenApiKeyOption.IsChecked)
                {
                    StopLoadingAndAlert(true,
                        "Unable to log into your Bitwarden account. Is your API key information correct?");
                }
                else
                {
                    StopLoadingAndAlert(true,
                        "Unable to log into your Bitwarden account. " +
                        "Try logging in using the API key option instead.");
                }
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

            if (!string.IsNullOrWhiteSpace(importFilePath) && !string.IsNullOrWhiteSpace(sessionKey))
            {
                var importSuccess = ImportCli(importOption, importFilePath, sessionKey);
                if (importSuccess)
                {
                    StopLoadingAndAlert(false, "Your import was successful!");
                    ClearInputs();
                }
                else
                {
                    StopLoadingAndAlert(true, "Something went wrong with the import.");
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

    private bool ConfigServerCli()
    {
        var (exitCode, stdOut, stdErr) = ExecCli($"config server {BitwardenServerUrl?.Text}");
        return exitCode == 0;
    }

    private (bool, string) LogInCli()
    {
        var (exitCode, sessionKey, error) = (1, string.Empty, string.Empty);
        // API key login
        if (BitwardenApiKeyOption.IsChecked)
        {
            (exitCode, sessionKey, error) = ExecCli("login --apikey --raw", (process) =>
            {
                process.StartInfo.EnvironmentVariables["BW_CLIENTID"] = BitwardenApiKeyClientId?.Text;
                process.StartInfo.EnvironmentVariables["BW_CLIENTSECRET"] = BitwardenApiKeySecret?.Text;
                // Avoid BW_NOINTERACTION bug that is issuing invalid session key on api key login
                process.StartInfo.EnvironmentVariables["BW_NOINTERACTION"] = "false";
            });
        }
        // Master password login
        else
        {
            var command = string.Format("login {0} {1} --raw", BitwardenEmail?.Text, BitwardenPassword?.Text);
            (exitCode, sessionKey, error) = ExecCli(command);
            if (exitCode == 1)
            {
                var promptMethod = error.Contains("no provider selected", StringComparison.CurrentCultureIgnoreCase);
                var promptCode = promptMethod || error.Contains("code is required", StringComparison.CurrentCultureIgnoreCase);
                int? method = null;
                string code = null;

                if (promptMethod)
                {
                    var methodTask = Dispatcher.DispatchAsync(() =>
                        DisplayActionSheet("Select Bitwarden 2FA method.", "Cancel", null,
                        _twoFactorMethods.Keys.ToArray()));
                    var methodKey = methodTask.GetAwaiter().GetResult();
                    if (!string.IsNullOrWhiteSpace(methodKey))
                    {
                        method = _twoFactorMethods[methodKey];
                    }
                }

                if (promptCode)
                {
                    var codeTask = Dispatcher.DispatchAsync(() =>
                        DisplayPromptAsync("Bitwarden Two-step Login", "Enter your two-step login code.", "Submit"));
                    code = codeTask.GetAwaiter().GetResult();
                }

                if (method.HasValue)
                {
                    command = string.Format("login {0} {1} --method {2} --code {3} --raw",
                        BitwardenEmail?.Text, BitwardenPassword?.Text, method.Value, code);
                }
                else
                {
                    command = string.Format("login {0} {1} --code {2} --raw",
                        BitwardenEmail?.Text, BitwardenPassword?.Text, code);
                }

                (exitCode, sessionKey, error) = ExecCli(command);
            }
        }
        return (exitCode == 0, sessionKey);
    }

    private (bool, string) UnlockCli()
    {
        var (exitCode, sessionKey, error) = ExecCli($"unlock {BitwardenPassword?.Text} --raw");
        return (exitCode == 0, sessionKey);
    }

    private bool ImportCli(string importService, string importFilePath, string sessionKey)
    {
        var (importExitCode, importStdOut, stdErr) = ExecCli($"import {importService} {importFilePath}", (process) =>
        {
            process.StartInfo.EnvironmentVariables["BW_SESSION"] = sessionKey;
        });
        return importExitCode == 0 && (importStdOut?.Contains("Imported") ?? false);
    }

    private async Task SetupCliAsync()
    {
        if (await HasLatestCliAsync())
        {
            return;
        }

        // Download CLI app to disk so that we can invoke it.

        var isWindows = DeviceInfo.Platform == DevicePlatform.WinUI;

        // Hash file
        var cliHashUrl = string.Format(
            "{0}/cli-v{1}/bw-{2}-sha256-{1}.txt",
            _cliBaseDownloadUrl, _cliVersion, isWindows ? "windows" : "macos");
        var cliHashFilename = Path.Combine(_cacheDir, "bw.sha256.txt");
        await DownloadFileAsync(cliHashUrl, cliHashFilename);

        // Zip file
        var cliUrl = string.Format("{0}/cli-v{1}/bw-{2}-{1}.zip",
            _cliBaseDownloadUrl, _cliVersion, isWindows ? "windows" : "macos");
        var cliZipFilename = Path.Combine(_cacheDir, "bw.zip");
        var cliPath = ResolveCliPath();
        await DownloadFileAsync(cliUrl, cliZipFilename);

        // Verify checksums
        using var hashFileStream = File.OpenRead(cliHashFilename);
        using var hashFileReader = new StreamReader(hashFileStream);
        var hashFileHex = hashFileReader.ReadToEnd().Trim();
        hashFileStream.Close();

        using var zipStream = File.OpenRead(cliZipFilename);
        using var sha256 = SHA256.Create();
        var zipHashBytes = sha256.ComputeHash(zipStream);
        var zipHashHex = BitConverter.ToString(zipHashBytes).Replace("-", string.Empty);
        zipStream.Close();

        if (!string.Equals(zipHashHex, hashFileHex, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception("CLI checksum failed.");
        }

        // Extract zip
        ZipFile.ExtractToDirectory(cliZipFilename, _cacheDir, true);

        // macOS permissions
        if (!isWindows)
        {
            ExecBash($"chmod +x {cliPath}");
        }
    }

    private (int, string, string) ExecCli(string args, Action<Process> processAction = null)
    {
        // Set up the process
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ResolveCliPath(),
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
        };

        // Load standard env vars for this use case
        process.StartInfo.EnvironmentVariables["BITWARDENCLI_APPDATA_DIR"] = _cacheDir;
        process.StartInfo.EnvironmentVariables["BW_NOINTERACTION"] = "true";
        processAction?.Invoke(process);

        process.Start();
        var stdOut = "";
        var stdErr = "";
        while (!process.StandardOutput.EndOfStream)
        {
            stdOut += process.StandardOutput.ReadLine();
        }
        while (!process.StandardError.EndOfStream)
        {
            stdErr += process.StandardError.ReadLine();
        }
        process.StandardOutput.Close();
        process.WaitForExit();
        return (process.ExitCode, stdOut.Trim(), stdErr.Trim());
    }

    private string ResolveCliPath()
    {
        var bwCliFilename = DeviceInfo.Platform == DevicePlatform.WinUI ? "bw.exe" : "bw";
        return Path.Combine(_cacheDir, bwCliFilename);
    }

    private Task CleanupAsync()
    {
        File.Delete(Path.Combine(_cacheDir, "data.json"));
        File.Delete(Path.Combine(_cacheDir, "export.csv"));
        File.Delete(Path.Combine(_cacheDir, "bw.zip"));
        return Task.FromResult(0);
    }

    private Task SetupAsync()
    {
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
        return Task.FromResult(0);
    }

    public static void ExecBash(string cmd)
    {
        var escapedArgs = cmd.Replace("\"", "\\\"");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\""
            }
        };
        process.Start();
        process.WaitForExit();
    }

    private void ClearInputs()
    {
        Dispatcher.Dispatch(() =>
        {
            BitwardenServerUrl.Text = string.Empty;
            BitwardenEmail.Text = string.Empty;
            BitwardenApiKeyOption.IsChecked = false;
            BitwardenApiKeyClientId.Text = string.Empty;
            BitwardenApiKeySecret.Text = string.Empty;
            BitwardenKeyConnector.IsChecked = false;
            BitwardenPassword.Text = string.Empty;
            LastPassEmail.Text = string.Empty;
            LastPassPassword.Text = string.Empty;
            OnePasswordEmail.Text = string.Empty;
            OnePasswordPassword.Text = string.Empty;
            OnePasswordSecretKey.Text = string.Empty;
            OnePasswordDomain.Text = string.Empty;
            OnePasswordExcludedVaults.Text = string.Empty;
        });
    }

    private void ParseCommandlineDefaults()
    {
        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (!arg.Contains('='))
            {
                continue;
            }

            var argParts = arg.Split(new[] { '=' }, 2);
            if (argParts.Length < 2)
            {
                continue;
            }

            if (argParts[0] == "bitwardenServerUrl")
            {
                BitwardenServerUrl.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "bitwardenEmail")
            {
                BitwardenEmail.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "bitwardenApiKey")
            {
                BitwardenApiKeyOption.IsChecked = argParts[1] == "1";
                continue;
            }

            if (argParts[0] == "bitwardenApiKeyClientId")
            {
                BitwardenApiKeyClientId.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "bitwardenApiKeySecret")
            {
                BitwardenApiKeySecret.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "bitwardenMasterPassword")
            {
                BitwardenPassword.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "bitwardenKeyConnector")
            {
                BitwardenKeyConnector.IsChecked = argParts[1] == "1";
                continue;
            }

            if (argParts[0] == "lastpassEmail")
            {
                LastPassEmail.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "lastpassMasterPassword")
            {
                LastPassPassword.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "lastpassSkipShared")
            {
                LastPassSkipShared.IsChecked = argParts[1] == "1";
                continue;
            }

            if (argParts[0] == "disableLastpassSkipShared")
            {
                LastPassSkipShared.IsEnabled = argParts[1] != "1";
                continue;
            }

            if (argParts[0] == "1passwordEmail")
            {
                OnePasswordEmail.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "1passwordPassword")
            {
                OnePasswordPassword.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "1passwordSecretKey")
            {
                OnePasswordSecretKey.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "1passwordDomain")
            {
                OnePasswordDomain.Text = argParts[1];
                continue;
            }

            if (argParts[0] == "1passwordExcludedVaults")
            {
                OnePasswordExcludedVaults.Text = argParts[1];
                continue;
            }
        }
    }

    private void Log(string message)
    {
        if (_doLogging)
        {
            File.AppendAllText(Path.Combine(_cacheDir, "log.txt"),
                $"[{DateTime.UtcNow}] {message}");
        }
    }

    private async Task<bool> HasLatestCliAsync()
    {
        var cliHashFilename = Path.Combine(_cacheDir, "bw.sha256.txt");
        if (!File.Exists(cliHashFilename) || !File.Exists(ResolveCliPath()))
        {
            return false;
        }

        // Read zip hash file on disk
        using var zipHashStream = File.OpenRead(cliHashFilename);
        using var zipHashReader = new StreamReader(zipHashStream);
        var zipHashHex = zipHashReader.ReadToEnd().Trim();

        // Download hash from latest CLI release
        var cliHashUrl = string.Format(
            "{0}/cli-v{1}/bw-{2}-sha256-{1}.txt",
            _cliBaseDownloadUrl, _cliVersion, DeviceInfo.Platform == DevicePlatform.WinUI ? "windows" : "macos");
        using var hashStream = await _httpClient.GetStreamAsync(cliHashUrl);
        using var reader = new StreamReader(hashStream);
        var hashHex = reader.ReadToEnd().Trim();

        // Close streams
        zipHashStream.Close();
        hashStream.Close();

        // Compare the hashes
        return string.Equals(zipHashHex, hashHex, StringComparison.InvariantCultureIgnoreCase);
    }

    private async Task DownloadFileAsync(string url, string file)
    {
        using var stream = await _httpClient.GetStreamAsync(url);
        using var fileStream = File.Create(file);
        await stream.CopyToAsync(fileStream);
        stream.Close();
        fileStream.Close();
    }
}
