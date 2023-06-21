namespace Bit.Importer.Services;

public class MemoryStorage : PasswordManagerAccess.Common.ISecureStorage
{
    private readonly Dictionary<string, string> _storage = new();

    public void StoreString(string name, string value)
    {
        if (value == null)
        {
            _storage.Remove(name);
        }
        else
        {
            _storage[name] = value;
        }
    }

    public string LoadString(string name)
    {
        return _storage.ContainsKey(name) ? _storage[name] : null;
    }
}
