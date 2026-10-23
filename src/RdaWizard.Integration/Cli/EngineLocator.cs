using Microsoft.Extensions.Logging;

namespace RdaWizard.Integration.Cli;

/// <summary>Locates the engine executable (rrecov). Search order:
/// 1. RDA_WIZARD_RRECOV_PATH env var (tests + CI),
/// 2. &lt;app&gt;\engine\rrecov(.exe),
/// 3. &lt;app&gt;\rrecov(.exe),
/// 4. PATH.</summary>
public interface IEngineLocator
{
    string? Locate();
}

public static class EngineNames
{
    public static string Binary => OperatingSystem.IsWindows() ? "rrecov.exe" : "rrecov";
}

public sealed class EngineLocator : IEngineLocator
{
    private readonly ILogger<EngineLocator> _logger;
    private string? _cached;

    public EngineLocator(ILogger<EngineLocator> logger) => _logger = logger;

    public string? Locate()
    {
        if (_cached is not null) return _cached;

        var candidates = new List<string>();

        var envOverride = Environment.GetEnvironmentVariable("RDA_WIZARD_RRECOV_PATH");
        if (!string.IsNullOrWhiteSpace(envOverride))
        {
            candidates.Add(envOverride);
        }

        var baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(baseDir, "engine", EngineNames.Binary));
        candidates.Add(Path.Combine(baseDir, EngineNames.Binary));

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                _cached = candidate;
                _logger.LogInformation("Engine located at {Path}", candidate);
                return candidate;
            }
        }

        // PATH fallback
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        foreach (var dir in pathVariable.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, EngineNames.Binary);
                if (File.Exists(candidate))
                {
                    _cached = candidate;
                    _logger.LogInformation("Engine located on PATH at {Path}", candidate);
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // Malformed PATH entries are skipped.
            }
        }

        _logger.LogError("Engine NOT located (searched env override, app dir, PATH) — scans cannot run");
        return null;
    }
}
