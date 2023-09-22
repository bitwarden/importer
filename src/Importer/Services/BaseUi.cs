using PasswordManagerAccess.Common;

namespace Bit.Importer.Services;

public class BaseUi : IDuoUi
{
    private readonly MainPage _page;
    private readonly string _serviceName;

    public BaseUi(MainPage page, string serviceName)
    {
        _page = page;
        _serviceName = serviceName;
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

    protected string PromptCode(string message)
    {
        var task = _page.Dispatcher.DispatchAsync(() =>
            _page.DisplayPromptAsync($"{_serviceName} Two-step Login", message, "Submit"));
        return task.GetAwaiter().GetResult();
    }
}
