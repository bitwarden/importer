using PasswordManagerAccess.LastPass;
using ServiceStack.Text;

namespace Bit.Importer.Services.LastPass;

public class LastPassImportService : IImportService
{
    private const string ImportOptionName = "lastpasscsv";

    private readonly MainPage _page;
    private readonly string _cacheDir;
    private readonly string _email;
    private readonly string _password;
    private readonly bool _skipShared;

    public LastPassImportService(MainPage page, string cacheDir,
        string email, string password, bool skipShared)
    {
        _page = page;
        _cacheDir = cacheDir;
        _email = email;
        _password = password;
        _skipShared = skipShared;
    }

    public async Task<(bool, string, string)> CreateImportFileAsync()
    {
        try
        {
            // Log in and get LastPass data
            var ui = new Ui(_page);
            var clientInfo = new ClientInfo(
                PasswordManagerAccess.LastPass.Platform.Desktop,
                Guid.NewGuid().ToString().ToLower(),
                "Importer");
            var vault = Vault.Open(_email, _password, clientInfo, ui,
                new ParserOptions { ParseSecureNotesToAccount = false });

            // Filter accounts
            var filteredAccounts = vault.Accounts.Where(a => !a.IsShared || (a.IsShared && !_skipShared));

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
}
