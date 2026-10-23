using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace RdaWizard.Core.Settings;

/// <summary>
/// Settings service: JSON file under the app-data root. Writes are atomic
/// (temp + move). Corrupt files fall back to defaults with a logged
/// diagnostic — never a crash, never silent garbage.
/// </summary>
public interface ISettingsService
{
    AppSettings Current { get; }
    event EventHandler<AppSettings>? Changed;
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
    /// <summary>App-data root (%LOCALAPPDATA%\RdaWizard on Windows; XDG equivalent elsewhere for tests).</summary>
    string AppDataRoot { get; }
    string SettingsFilePath { get; }
}

public sealed class SettingsService : ISettingsService
{
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly object _gate = new();
    private AppSettings _current = AppSettings.Default;

    public SettingsService(ILogger<SettingsService> logger, string? appDataRootOverride = null)
    {
        _logger = logger;
        AppDataRoot = appDataRootOverride ?? ResolveDefaultRoot();
        SettingsFilePath = Path.Combine(AppDataRoot, FileName);
    }

    public string AppDataRoot { get; }

    public string SettingsFilePath { get; }

    public AppSettings Current
    {
        get { lock (_gate) return _current; }
    }

    public event EventHandler<AppSettings>? Changed;

    public Task LoadAsync(CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            AppSettings loaded;
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    loaded = AppSettings.Default;
                    _logger.LogInformation("No settings file at {Path}; defaults in effect", SettingsFilePath);
                }
                else
                {
                    var json = File.ReadAllText(SettingsFilePath, System.Text.Encoding.UTF8);
                    loaded = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? AppSettings.Default;
                    _logger.LogInformation("Settings loaded from {Path}", SettingsFilePath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex, "Settings file unreadable at {Path}; defaults in effect", SettingsFilePath);
                loaded = AppSettings.Default;
            }

            lock (_gate) _current = loaded;
            Changed?.Invoke(this, loaded);
        }, cancellationToken);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            Directory.CreateDirectory(AppDataRoot);
            var tempPath = SettingsFilePath + ".tmp";
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (File.Exists(SettingsFilePath))
            {
                File.Replace(tempPath, SettingsFilePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, SettingsFilePath);
            }

            lock (_gate) _current = settings;
            _logger.LogInformation("Settings saved to {Path}", SettingsFilePath);
            Changed?.Invoke(this, settings);
        }, cancellationToken);

    private static string ResolveDefaultRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
        {
            localAppData = Path.GetTempPath();
        }
        return Path.Combine(localAppData, "RdaWizard");
    }
}
