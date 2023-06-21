using PasswordManagerAccess.OnePassword.Ui;

namespace Bit.Importer.Services.OnePassword;

public class Ui : BaseUi, IUi
{
    public Ui(MainPage page)
        : base(page) { }

    public Passcode ProvideGoogleAuthPasscode()
    {
        return new Passcode(PromptCode("Enter your authenticator two-step login code."), false);
    }

    public Passcode ProvideWebAuthnRememberMe()
    {
        return new Passcode(PromptCode("Enter your WebAuthn two-step login code."), false);
    }
}
