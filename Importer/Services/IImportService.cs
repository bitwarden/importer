namespace Bit.Importer.Services;

public interface IImportService
{
    Task<(bool, string, string)> CreateImportFileAsync();
}
