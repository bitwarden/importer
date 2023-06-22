using PasswordManagerAccess.LastPass.Ui;

namespace Bit.Importer.Services.LastPass;

public class Ui : BaseUi, IUi
{
    public Ui(MainPage page)
        : base(page, "LastPass") { }

    public OtpResult ProvideGoogleAuthPasscode()
    {
        return new OtpResult(PromptCode("Enter your authenticator two-step login code."), false);
    }

    public OtpResult ProvideMicrosoftAuthPasscode()
    {
        return new OtpResult(PromptCode("Enter your authenticator two-step login code."), false);
    }

    public OtpResult ProvideYubikeyPasscode()
    {
        return new OtpResult(PromptCode("Enter your Yubikey code."), false);
    }

    public OobResult ApproveLastPassAuth()
    {
        return OobResult.ContinueWithPasscode(PromptCode("Enter passcode from LastPass Authenticator."), false);
    }

    public OobResult ApproveDuo()
    {
        return OobResult.ContinueWithPasscode(PromptCode("Enter passcode from Duo."), false);
    }

    public OobResult ApproveSalesforceAuth()
    {
        return OobResult.ContinueWithPasscode(PromptCode("Enter passcode from Salesforce Authenticator."), false);
    }
}
