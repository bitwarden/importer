namespace Bit.Importer;

public partial class App : Application
{
    public App()
    {
        // Uncomment below to force light theme
        // Current.UserAppTheme = AppTheme.Light;
        InitializeComponent();

        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Width = 650;
        window.Height = 1150;
        window.Title = "Bitwarden Importer";

        if (DeviceInfo.Platform != DevicePlatform.WinUI)
        {
            window.MinimumWidth = 650;
            window.MinimumHeight = 1150;

            Dispatcher.DispatchAsync(async () =>
            {
                await Task.Delay(1000);
                window.MinimumWidth = 0;
                window.MinimumHeight = 0;
                window.MaximumWidth = double.PositiveInfinity;
                window.MaximumHeight = double.PositiveInfinity;
            });
        }

        return window;
    }
}
