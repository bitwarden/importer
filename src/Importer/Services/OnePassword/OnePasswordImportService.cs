using PasswordManagerAccess.OnePassword;
using ServiceStack.Text;

namespace Bit.Importer.Services.OnePassword;

internal class OnePasswordImportService : IImportService
{
    private const string ImportOptionName = "1passwordmaccsv";
    private static Random _random = new();

    private readonly MainPage _page;
    private readonly string _cacheDir;
    private readonly string _email;
    private readonly string _accountKey;
    private readonly string _password;
    private readonly string _domain;
    private readonly List<string> _excludedVaultNames;

    public OnePasswordImportService(MainPage page, string cacheDir, string email, string accountKey,
        string password, string domain, string excludedVaultNames)
    {
        _page = page;
        _cacheDir = cacheDir;
        _email = email;
        _accountKey = accountKey;
        _password = password;
        _domain = domain;
        _excludedVaultNames = excludedVaultNames?
            .Split(",")
            .Select(n => n.Trim().ToLowerInvariant())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList() ?? new List<string>();
    }

    public async Task<(bool, string, string)> CreateImportFileAsync()
    {
        try
        {
            var filteredAccounts = new List<Account>();

            // Log in and get 1Password data
            var ui = new Ui(_page);
            var clientInfo = new ClientInfo
            {
                Username = _email,
                Password = _password,
                AccountKey = _accountKey,
                Uuid = RandomString(26).ToLowerInvariant(),
                Domain = string.IsNullOrWhiteSpace(_domain) ?
                    PasswordManagerAccess.OnePassword.Region.Global.ToDomain() : _domain,
                DeviceName = "Importer",
                DeviceModel = "1.0.0",
            };

            var session = Client.LogIn(clientInfo, ui, new MemoryStorage());
            try
            {
                var vaults = Client.ListAllVaults(session);
                foreach (var vaultInfo in vaults)
                {
                    // Skip vaults we want to exclude
                    if (string.IsNullOrWhiteSpace(vaultInfo.Name) ||
                        _excludedVaultNames.Contains(vaultInfo.Name.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var vault = Client.OpenVault(vaultInfo, session);
                    filteredAccounts.AddRange(vault.Accounts);
                }
            }
            finally
            {
                Client.LogOut(session);
            }

            // Massage it to expected CSV format
            var exportAccounts = filteredAccounts.Select(a => new ExportedAccount(a));

            // Create CSV string
            var csvOutput = CsvSerializer.SerializeToCsv(exportAccounts);

            // Write CSV to temp disk
            var csvPath = Path.Combine(_cacheDir, "export.csv");
            await File.WriteAllTextAsync(csvPath, csvOutput);
            return (true, csvPath, ImportOptionName);
        }
        catch
        {
            return (false, null, ImportOptionName);
        }
    }

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
