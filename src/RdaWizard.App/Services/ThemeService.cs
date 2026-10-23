using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Settings;

namespace RdaWizard.App.Services;

/// <summary>
/// Live theme switching: swaps the active theme dictionary in
/// Application.Resources (light/dark/high-contrast/system). System follows Windows
/// AppsUseLightTheme via registry read (no WMI dependency) and auto-engages the
/// high-contrast dictionary when the OS high-contrast accessibility flag is on.
/// </summary>
public sealed class ThemeService
{
    private const string LightPack = "pack://application:,,,/RdaWizard.Design;component/Themes/Theme.Light.xaml";
    private const string DarkPack = "pack://application:,,,/RdaWizard.Design;component/Themes/Theme.Dark.xaml";
    private const string HighContrastPack = "pack://application:,,,/RdaWizard.Design;component/Themes/Theme.HighContrast.xaml";

    private readonly ILogger<ThemeService> _logger;

    public ThemeService(ILogger<ThemeService> logger) => _logger = logger;

    public string Current { get; private set; } = "light";

    public void Apply(string theme)
    {
        // High contrast always wins over the requested palette (a11y requirement,
        // VISUAL-SPEC.md §2.2) unless the caller explicitly pins a concrete theme.
        string effective;
        string pack;
        if (theme == "highcontrast" || (theme is not ("light" or "dark") && IsSystemHighContrast()))
        {
            effective = "highcontrast";
            pack = HighContrastPack;
        }
        else
        {
            effective = theme switch
            {
                "dark" => "dark",
                "light" => "light",
                _ => IsSystemDark() ? "dark" : "light",
            };
            pack = effective == "dark" ? DarkPack : LightPack;
        }

        var dictionary = new ResourceDictionary { Source = new Uri(pack) };

        var merged = Application.Current.Resources.MergedDictionaries;
        // The theme dictionary is the LAST entry (see App.xaml); replace it.
        if (merged.Count > 0)
        {
            merged[^1] = dictionary;
        }
        else
        {
            merged.Add(dictionary);
        }

        Current = effective;
        _logger.LogInformation("Theme applied: {Theme} (requested {Requested})", effective, theme);
    }

    private static bool IsSystemHighContrast()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\Themes");
            // The presence of an active high-contrast theme is surfaced via the
            // SystemParameters flag; registry is the fallback probe.
            if (key?.GetValue("CurrentTheme") is string themeName)
            {
                return !string.IsNullOrEmpty(themeName)
                    && themeName.Contains("hc", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("High-contrast probe failed: " + ex.Message);
        }
        return SystemParameters.HighContrast;
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0;
            }
        }
        catch (Exception ex)
        {
            // Registry read failures default to light.
            System.Diagnostics.Debug.WriteLine("Personalize key read failed: " + ex.Message);
        }
        return false;
    }
}

/// <summary>
/// Debug dispatcher watchdog (budget: no UI-thread blocking > 50 ms).
/// Pings the dispatcher at background priority; if a ping is not serviced
/// within the threshold, logs a warning with the stall duration.
/// </summary>
public sealed class DispatcherWatchdog : IDisposable
{
    private readonly ILogger<DispatcherWatchdog> _logger;
    private readonly System.Windows.Threading.Dispatcher _dispatcher;

    private const int ThresholdMs = 50;
    private const int IntervalMs = 250;
    private System.Threading.Timer? _timer;
    private long _lastTick;

    public DispatcherWatchdog(ILogger<DispatcherWatchdog> logger)
        : this(logger, null)
    {
    }

    public DispatcherWatchdog(ILogger<DispatcherWatchdog> logger, System.Windows.Threading.Dispatcher? dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher ?? System.Windows.Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("DispatcherWatchdog requires a dispatcher (construct on the UI thread).");
    }

    public void Start()
    {
        _lastTick = Environment.TickCount64;
        _timer = new System.Threading.Timer(_ =>
        {
            var observed = Environment.TickCount64;
            var last = Interlocked.Exchange(ref _lastTick, observed);
            var stalledFor = observed - last;
            if (stalledFor > ThresholdMs + IntervalMs)
            {
                _logger.LogWarning("UI thread stalled {Stalled} ms (budget: {Threshold} ms)", stalledFor, ThresholdMs);
            }
            _dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => Interlocked.Exchange(ref _lastTick, Environment.TickCount64)));
        }, null, IntervalMs, IntervalMs);
        _logger.LogDebug("Dispatcher watchdog started (threshold {Threshold} ms)", ThresholdMs);
    }

    public void Stop() => _timer?.Dispose();

    public void Dispose() => _timer?.Dispose();
}
