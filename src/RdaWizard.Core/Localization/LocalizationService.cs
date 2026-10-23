using Microsoft.Extensions.Logging;

namespace RdaWizard.Core.Localization;

/// <summary>
/// Localization infrastructure. Original English strings only (clean-room
/// rule — no proprietary strings copied); zh-CN translations ship alongside.
/// The default culture falls back to English for any missing key.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Current culture tag ("en" or "zh-CN").</summary>
    string CurrentCulture { get; }

    event EventHandler<string>? CultureChanged;

    /// <summary>Sets the culture ("en", "zh-CN", or "system" resolved to the OS culture).</summary>
    void SetCulture(string cultureTag);

    /// <summary>Looks up a key in the current culture, falling back to English, then to the key itself.</summary>
    string this[string key] { get; }

    /// <summary>Formatted lookup with args.</summary>
    string Format(string key, params object[] args);
}

/// <summary>
/// Resx-free, JSON-backed string table. Keys are stable identifiers
/// (<c>Home.Title</c> etc.); tables live beside the executable as
/// <c>strings/en.json</c>, <c>strings/zh-CN.json</c>. Missing files degrade
/// to key-as-text with a logged warning — the UI never crashes on language
/// data.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly Microsoft.Extensions.Logging.ILogger<LocalizationService> _logger;
    private readonly string _stringsDirectory;
    private readonly Dictionary<string, string> _english = new(StringComparer.Ordinal);
    private Dictionary<string, string> _currentTable = new(StringComparer.Ordinal);
    private string _culture = "en";

    public LocalizationService(Microsoft.Extensions.Logging.ILogger<LocalizationService> logger, string? stringsDirectoryOverride = null)
    {
        _logger = logger;
        _stringsDirectory = stringsDirectoryOverride ?? ResolveDefaultDirectory();
        LoadCulture("en", _english);
        _currentTable = _english;
    }

    public string CurrentCulture => _culture;

    public event EventHandler<string>? CultureChanged;

    public string this[string key]
    {
        get
        {
            if (_currentTable.TryGetValue(key, out var value)) return value;
            if (_english.TryGetValue(key, out var fallback))
            {
                _logger.LogWarning("Missing localization '{Key}' in {Culture}; falling back to English", key, _culture);
                return fallback;
            }
            _logger.LogWarning("Unknown localization key '{Key}'; returning the key", key);
            return key;
        }
    }

    public string Format(string key, params object[] args)
    {
        var format = this[key];
        try
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
        }
        catch (FormatException)
        {
            _logger.LogWarning("Malformed format string for key '{Key}'", key);
            return format;
        }
    }

    public void SetCulture(string cultureTag)
    {
        var resolved = Resolve(cultureTag);
        if (string.Equals(resolved, _culture, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _culture = resolved;
        _currentTable = new Dictionary<string, string>(_english, StringComparer.Ordinal);
        if (!string.Equals(resolved, "en", StringComparison.OrdinalIgnoreCase))
        {
            var overlay = new Dictionary<string, string>(StringComparer.Ordinal);
            LoadCulture(resolved, overlay);
            foreach (var pair in overlay)
            {
                _currentTable[pair.Key] = pair.Value;
            }
        }

        _logger.LogInformation("Culture set to {Culture} ({Keys} keys)", _culture, _currentTable.Count);
        CultureChanged?.Invoke(this, _culture);
    }

    private static string Resolve(string cultureTag)
    {
        if (string.Equals(cultureTag, "system", StringComparison.OrdinalIgnoreCase))
        {
            var name = System.Globalization.CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
            return "en";
        }
        if (cultureTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
        return "en";
    }

    private void LoadCulture(string culture, Dictionary<string, string> into)
    {
        var path = Path.Combine(_stringsDirectory, culture + ".json");
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("String table missing: {Path}", path);
                return;
            }

            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                into[property.Name] = property.Value.GetString() ?? string.Empty;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "String table unreadable: {Path}", path);
        }
    }

    private static string ResolveDefaultDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, "strings");
        return Directory.Exists(candidate) ? candidate : baseDir;
    }
}
