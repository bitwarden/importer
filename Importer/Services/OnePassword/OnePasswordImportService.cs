using PasswordManagerAccess.OnePassword;

namespace Bit.Importer.Services.OnePassword;

internal class OnePasswordImportService : IImportService
{
    private const string ImportOptionName = "1passwordmaccsv";

    private readonly MainPage _page;
    private readonly string _cacheDir;

    public OnePasswordImportService(MainPage page, string cacheDir)
    {
        _page = page;
        _cacheDir = cacheDir;
    }

    public async Task<(bool, string, string)> CreateImportFileAsync()
    {
        try
        {
            var ui = new Ui(_page);
            var clientInfo = new ClientInfo
            {
                Username = "",
                Password = "",
                AccountKey = "",
                Uuid = Guid.NewGuid().ToString().ToLower(),
                Domain = "",
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
        catch
        {
            return (false, null, ImportOptionName);
        }
    }
}
