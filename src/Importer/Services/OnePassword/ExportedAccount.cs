using System.Runtime.Serialization;
using PasswordManagerAccess.OnePassword;

namespace Bit.Importer.Services.OnePassword;

[DataContract]
public class ExportedAccount
{
    public ExportedAccount() { }

    public ExportedAccount(Account account)
    {
        Title = account.Name;
        Urls = account.MainUrl;
        Username = account.Username;
        Password = account.Password;
        Notes = account.Note;
    }

    [DataMember(Name = "title")]
    public string Title { get; set; }
    [DataMember(Name = "urls")]
    public string Urls { get; set; }
    [DataMember(Name = "username")]
    public string Username { get; set; }
    [DataMember(Name = "password")]
    public string Password { get; set; }
    [DataMember(Name = "notes")]
    public string Notes { get; set; }
}
