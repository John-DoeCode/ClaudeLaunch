using System.IO;
using System.Windows;
using ClaudeLauncher.Views;

namespace ClaudeLauncher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Supported arguments:
        //   <folder>   select this folder on start
        //   --auto     launch the selected project immediately
        string? folder = null;
        var autoLaunch = false;

        foreach (var arg in e.Args)
        {
            if (string.Equals(arg, "--auto", StringComparison.OrdinalIgnoreCase))
            {
                autoLaunch = true;
            }
            else if (Directory.Exists(arg))
            {
                folder = arg;
            }
        }

        var window = new MainWindow(folder, autoLaunch);
        MainWindow = window;
        window.Show();
    }
}
