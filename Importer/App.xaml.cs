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
        window.Height = 1100;
        window.Title = "Bitwarden Importer";
        return window;
    }
}
