using System.IO;
using System.Windows;
using ClaudeLauncher.ViewModels;

namespace ClaudeLauncher.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly bool _autoLaunch;

    public MainWindow() : this(null, false)
    {
    }

    public MainWindow(string? folder, bool autoLaunch)
    {
        InitializeComponent();
        DataContext = _viewModel;
        _autoLaunch = autoLaunch;

        if (folder is not null)
        {
            _viewModel.SelectFolder(folder);
        }

        _viewModel.LaunchCompleted += OnLaunchCompleted;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_autoLaunch && _viewModel.LaunchCommand.CanExecute(null))
        {
            _viewModel.LaunchCommand.Execute(null);
        }
    }

    /// <summary>Every action succeeded: close the launcher without asking.</summary>
    private void OnLaunchCompleted(object? sender, EventArgs e) => Close();

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedFolder(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (GetDroppedFolder(e) is { } folder)
        {
            _viewModel.SelectFolder(folder);
        }
    }

    /// <summary>Accepts a dropped folder, or the parent folder of a dropped file.</summary>
    private static string? GetDroppedFolder(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } paths)
        {
            return null;
        }

        var path = paths[0];
        if (Directory.Exists(path))
        {
            return path;
        }

        return File.Exists(path) ? Path.GetDirectoryName(path) : null;
    }
}
