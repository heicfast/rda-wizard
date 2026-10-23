using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Services;

namespace RdaWizard.App.Services;

/// <summary>
/// WPF dialog service implementation: confirmations (destructive styling),
/// alerts, folder/file pickers. All dialogs run on the UI thread via the
/// dispatcher; ViewModels await them without blocking it.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly ILogger<DialogService> _logger;

    public DialogService(ILogger<DialogService> logger) => _logger = logger;

    public Task<DialogResult> ConfirmAsync(string title, string message, string confirmLabel, bool destructive = false)
        => RunOnUi(() =>
        {
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(static w => w.IsActive)
                        ?? Application.Current?.MainWindow;
            var text = owner is null ? message : message;
            var result = System.Windows.MessageBox.Show(
                owner, text, title,
                MessageBoxButton.OKCancel,
                destructive ? MessageBoxImage.Warning : MessageBoxImage.Question,
                MessageBoxResult.Cancel);
            _logger.LogInformation("Confirm '{Title}' -> {Result}", title, result);
            return result == MessageBoxResult.OK ? DialogResult.Confirmed : DialogResult.Cancelled;
        });

    public Task AlertAsync(string title, string message)
        => RunOnUi(() =>
        {
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(static w => w.IsActive)
                        ?? Application.Current?.MainWindow;
            System.Windows.MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
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
