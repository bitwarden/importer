using PasswordManagerAccess.OnePassword;

namespace Bit.Importer.Services.OnePassword;

internal class OnePasswordImportService : IImportService
{
    private const string ImportOptionName = "1passwordmaccsv";
    private static Random random = new Random();

    private readonly MainPage _page;
    private readonly string _cacheDir;
    private readonly string _email;
    private readonly string _accountKey;
    private readonly string _password;
    private readonly string _domain;

    public OnePasswordImportService(MainPage page, string cacheDir, string email, string accountKey,
        string password, string domain)
    {
        _page = page;
        _cacheDir = cacheDir;
        _email = email;
        _accountKey = accountKey;
        _password = password;
        _domain = domain;
    }

    public async Task<(bool, string, string)> CreateImportFileAsync()
    {
        try
        {
            var ui = new Ui(_page);
            var clientInfo = new ClientInfo
            {
                Username = _email,
                Password = _password,
                AccountKey = _accountKey,
                Uuid = RandomString(26).ToLower(),
                Domain = string.IsNullOrWhiteSpace(_domain) ?
                    PasswordManagerAccess.OnePassword.Region.Global.ToDomain() : _domain,
                DeviceName = "Importer",
                DeviceModel = "1.0.0",
            };

            var session = Client.LogIn(clientInfo, ui, new MemoryStorage());
            try
            {
                var vaults = Client.ListAllVaults(session);

                // TODO: filter vaults?

                foreach (var vaultInfo in vaults)
                {
                    var vault = Client.OpenVault(vaultInfo, session);
                    foreach (var account in vault.Accounts)
                    {

                    }
                }
            }
            finally
            {
                Client.LogOut(session);
            }

            return (true, "", ImportOptionName);
        }
        catch (Exception e)
        {
            return (false, null, ImportOptionName);
        }
    }

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
