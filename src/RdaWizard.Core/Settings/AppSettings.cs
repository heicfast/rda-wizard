namespace RdaWizard.Core.Settings;

/// <summary>User settings persisted as JSON (H. Settings &amp; About scope).</summary>
public sealed record AppSettings
{
    /// <summary>Theme: "light" | "dark" | "system".</summary>
    public string Theme { get; init; } = "system";

    /// <summary>Language: "en" | "zh-CN" | "system".</summary>
    public string Language { get; init; } = "system";

    /// <summary>Default scan mode ("quick" | "deep" | "deep+carve").</summary>
    public string DefaultScanMode { get; init; } = "deep+carve";

    /// <summary>Default recovery destination (null = ask each time).</summary>
    public string? DefaultRecoveryDestination { get; init; }

    /// <summary>Collision policy: "rename" | "skip" | "ask".</summary>
    public string CollisionPolicy { get; init; } = "rename";

    /// <summary>Preserve folder structure during recovery (destination tree mirroring).</summary>
    public bool PreserveFolderStructure { get; init; } = true;

    /// <summary>Log directory override (null = %LOCALAPPDATA%\RdaWizard\logs).</summary>
    public string? LogDirectory { get; init; }

    /// <summary>Preview caps: max bytes read into a preview (default 100 MiB — og parity).</summary>
    public long PreviewMaxBytes { get; init; } = 100L * 1024 * 1024;

    /// <summary>Preview caps: worker time budget (default 15 s — og parity).</summary>
    public int PreviewTimeoutSeconds { get; init; } = 15;

    /// <summary>Level of detail in the log (Serilog minimum level).</summary>
    public string LogLevel { get; init; } = "Information";

    public static AppSettings Default { get; } = new();

    public AppSettings With(string? theme = null, string? language = null, string? scanMode = null,
        string? destination = null, string? collision = null, bool? preserve = null,
        string? logDir = null, long? previewBytes = null, int? previewSeconds = null, string? logLevel = null)
        => new()
        {
            Theme = theme ?? Theme,
            Language = language ?? Language,
            DefaultScanMode = scanMode ?? DefaultScanMode,
            DefaultRecoveryDestination = destination ?? DefaultRecoveryDestination,
            CollisionPolicy = collision ?? CollisionPolicy,
            PreserveFolderStructure = preserve ?? PreserveFolderStructure,
            LogDirectory = logDir ?? LogDirectory,
            PreviewMaxBytes = previewBytes ?? PreviewMaxBytes,
            PreviewTimeoutSeconds = previewSeconds ?? PreviewTimeoutSeconds,
            LogLevel = logLevel ?? LogLevel,
        };
}
