using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using RdaWizard.App.Dialogs;
using RdaWizard.Core.Services;

namespace RdaWizard.App.Services;

/// <summary>
/// WPF dialog service: og-styled modals (AppModalDialog — custom button
/// labels, destructive styling; the DrwMessageBox 540-family form) and
/// native folder/file pickers. All dialogs run on the UI thread; VMs await
/// without blocking it.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly ILogger<DialogService> _logger;

    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
    }

    public Task<DialogResult> ConfirmAsync(string title, string message, string confirmLabel, bool destructive = false)
        => RunOnUi(() =>
        {
            var owner = ActiveWindow();
            var dialog = new AppModalDialog
            {
                Owner = owner,
                DialogTitle = title,
                Message = message,
                ConfirmLabel = string.IsNullOrWhiteSpace(confirmLabel) ? "OK" : confirmLabel,
                CancelLabel = "Continue",
                Destructive = destructive,
            };
            var confirmed = dialog.ShowDialog() == true && dialog.Confirmed;
            _logger.LogInformation("Confirm '{Title}' [{Confirm}] -> {Result}", title, confirmLabel, confirmed);
            return confirmed ? DialogResult.Confirmed : DialogResult.Cancelled;
        });

    public Task AlertAsync(string title, string message)
        => RunOnUi(() =>
        {
            var owner = ActiveWindow();
            var dialog = new AppModalDialog
            {
                Owner = owner,
                DialogTitle = title,
                Message = message,
                ConfirmLabel = "OK",
                CancelLabel = string.Empty,
            };
            dialog.CancelButton.Visibility = Visibility.Collapsed;
            dialog.ShowDialog();
            return true;
        });

    public Task<string?> PickFolderAsync(string title)
        => RunOnUi(() =>
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = title,
            };
            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }
            return null;
        });

    public Task<string?> PickOpenFileAsync(string title, string filter)
        => RunOnUi(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter,
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });

    private static Window? ActiveWindow() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(static w => w.IsActive)
        ?? Application.Current?.MainWindow;

    private static Task<T> RunOnUi<T>(Func<T> function)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            return Task.FromResult(function());
        }
        return Task.Run(() => dispatcher.Invoke(function));
    }
}
