using PasswordManagerAccess.LastPass;
using System.Runtime.Serialization;

namespace Bit.Importer.Services.LastPass;

[DataContract]
public class ExportedAccount
{
    public ExportedAccount() { }

    public ExportedAccount(Account account)
    {
        Url = account.Url;
        Username = account.Username;
        Password = account.Password;
        Totp = account.Totp;
        Extra = account.Notes;
        Name = account.Name;
        Grouping = account.Path == "(none)" ? null : account.Path;
        Fav = account.IsFavorite ? 1 : 0;
    }

    [DataMember(Name = "url")]
    public string Url { get; set; }
    [DataMember(Name = "username")]
    public string Username { get; set; }
    [DataMember(Name = "password")]
    public string Password { get; set; }
    [DataMember(Name = "totp")]
    public string Totp { get; set; }
    [DataMember(Name = "extra")]
    public string Extra { get; set; }
    [DataMember(Name = "name")]
    public string Name { get; set; }
    [DataMember(Name = "grouping")]
    public string Grouping { get; set; }
    [DataMember(Name = "fav")]
    public int Fav { get; set; }
}
