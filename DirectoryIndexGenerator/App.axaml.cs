using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DirectoryIndexGenerator;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = new MainWindow();
            // Set window icon from embedded asset
            var icon = new Avalonia.Media.Imaging.Bitmap(
                Avalonia.Platform.AssetLoader.Open(
                    new Uri("avares://DirectoryIndexGenerator/Assets/icon.png")));
            win.Icon = new Avalonia.Controls.WindowIcon(icon);
            desktop.MainWindow = win;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
