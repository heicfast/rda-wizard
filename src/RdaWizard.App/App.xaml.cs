using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdaWizard.Core;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;
using RdaWizard.Core.Wizard;
using RdaWizard.Integration;
using RdaWizard.App.Services;
using RdaWizard.App.Views;
using Serilog;

namespace RdaWizard.App;

/// <summary>
/// Generic Host composition root. ViewModels/services from Core +
/// Integration; WPF-facing services (dialogs, theme, launcher) implemented
/// here. Per-session Serilog file logging — logs are first-class.
/// </summary>
public partial class App : Application
{
    private static IHost? _host;
    private DispatcherWatchdog? _watchdog;

    public static IServiceProvider Services =>
        (_host ?? throw new InvalidOperationException("Host not initialized")).Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RdaWizard", "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDirectory, "rda-wizard-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddRdaWizardCore();
                services.AddRdaWizardIntegration();

                // WPF-facing services
                services.AddSingleton<Core.Services.IDialogService, Services.DialogService>();
                services.AddSingleton<ThemeService>();
                services.AddSingleton<ShellOrchestrator>();
                services.AddSingleton<DispatcherWatchdog>();
            })
            .Build();

        await _host.StartAsync().ConfigureAwait(true);

        // Marshal engine-thread VM updates onto the UI thread.
        ViewModelBase.InstallUiMarshaler(action =>
            Dispatcher.BeginInvoke(DispatcherPriority.DataBind, action));

        // Dispatcher watchdog: no UI-thread block > 50 ms (budget T19).
        // Constructed always (Release included); pings only run when started.
        _watchdog = Services.GetRequiredService<DispatcherWatchdog>();
#if DEBUG
        _watchdog.Start();
#endif

        var settings = Services.GetRequiredService<Core.Settings.ISettingsService>();
        await settings.LoadAsync().ConfigureAwait(true);

        var localization = Services.GetRequiredService<Core.Localization.ILocalizationService>();
        localization.SetCulture(settings.Current.Language);

        var theme = Services.GetRequiredService<ThemeService>();
        theme.Apply(settings.Current.Theme);

        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        MainWindow = mainWindow;

        var orchestrator = Services.GetRequiredService<ShellOrchestrator>();
        orchestrator.Wire(mainViewModel);

        mainWindow.Show();

        Log.Information("RdaWizard started (version {Version})", typeof(App).Assembly.GetName().Version);
        _ = mainViewModel.Home.RefreshAsync(CancellationToken.None);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _watchdog?.Stop();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            _host.Dispose();
        }
        Log.Information("RdaWizard exited with code {ExitCode}", e.ApplicationExitCode);
        await Log.CloseAndFlushAsync().ConfigureAwait(true);
        base.OnExit(e);
    }

    private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled UI exception");
        MessageBox.Show(
            "An unexpected error occurred. The application will continue if possible.\nDetails were written to the log.",
            "RDA Recovery Wizard",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
