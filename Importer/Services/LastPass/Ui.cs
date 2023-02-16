using PasswordManagerAccess.Common;
using PasswordManagerAccess.LastPass.Ui;

namespace Bit.Importer.Services.LastPass;

public class Ui : IUi
{
    private readonly MainPage _page;

    public Ui(MainPage page)
    {
        _page = page;
    }

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
        return OobResult.ContinueWithPasscode(PromptCode("Enter passcode from LastPass authenticator."), false);
    }

    public OobResult ApproveDuo()
    {
        return OobResult.ContinueWithPasscode(PromptCode("Enter passcode from Duo."), false);
    }

    public DuoChoice ChooseDuoFactor(DuoDevice[] devices)
    {
        var task = _page.Dispatcher.DispatchAsync(() =>
            _page.DisplayActionSheet("Choose a Duo device", "Cancel", null, devices.Select(d => d.Name).ToArray()));
        var actionSelection = task.GetAwaiter().GetResult();
        var device = devices.FirstOrDefault(d => d.Name == actionSelection);
        return new DuoChoice(device, DuoFactor.Passcode, false);
    }

    public string ProvideDuoPasscode(DuoDevice device)
    {
        return PromptCode("Enter your Duo passcode.");
    }

    public void UpdateDuoStatus(DuoStatus status, string text)
    {
        // Not sure what this is for.
    }

    private string PromptCode(string message)
    {
        var task = _page.Dispatcher.DispatchAsync(() =>
            _page.DisplayPromptAsync("LastPass Two-step Login", message, "Submit"));
        return task.GetAwaiter().GetResult();
    }
}
