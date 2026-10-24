using System.Windows;
using System.Windows.Input;

namespace ClaudeLauncher.Views;

/// <summary>
/// Small themed dialog used both to ask for a name and to show a message.
/// </summary>
public partial class InputDialog : Window
{
    private InputDialog()
    {
        InitializeComponent();
    }

    public string Value => InputBox.Text.Trim();

    /// <summary>Asks the user for a text, returns null when cancelled or left empty.</summary>
    public static string? AskText(Window? owner, string title, string message, string defaultValue = "")
    {
        var dialog = Create(owner, title, message);
        dialog.InputBox.Visibility = Visibility.Visible;
        dialog.InputBox.Text = defaultValue;
        dialog.Loaded += (_, _) =>
        {
            dialog.InputBox.Focus();
            dialog.InputBox.SelectAll();
        };

        return dialog.ShowDialog() == true && dialog.Value.Length > 0 ? dialog.Value : null;
    }

    /// <summary>Shows a message with a single button.</summary>
    public static void ShowMessage(Window? owner, string title, string message)
    {
        var dialog = Create(owner, title, message);
        dialog.CancelButton.Visibility = Visibility.Collapsed;
        dialog.OkButton.Content = "Compris";
        dialog.ShowDialog();
    }

    /// <summary>Asks for a confirmation, returns true when the user accepts.</summary>
    public static bool Confirm(Window? owner, string title, string message, string okText = "Confirmer")
    {
        var dialog = Create(owner, title, message);
        dialog.OkButton.Content = okText;
        return dialog.ShowDialog() == true;
    }

    private static InputDialog Create(Window? owner, string title, string message)
    {
        var dialog = new InputDialog
        {
            Owner = owner ?? Application.Current.MainWindow,
            Title = title
        };

        if (dialog.Owner is null)
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        return dialog;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DialogResult = true;
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
        }
    }
}
