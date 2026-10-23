namespace RdaWizard.Core.Models;

/// <summary>Health report parsed from <c>rrecov health --disk N</c> (docs/CORE-CONTRACT.md §13).</summary>
public sealed record HealthReport
{
    public required int DiskIndex { get; init; }

    /// <summary>SMART attribute rows (ATA). Empty for NVMe-only or unavailable bridges (gap G-16).</summary>
    public required IReadOnlyList<SmartAttribute> Attributes { get; init; }

    /// <summary>Aggregated key indicators (ATA). Null when SMART unavailable.</summary>
    public SmartAggregates? Aggregates { get; init; }

    /// <summary>NVMe health block. Null for ATA disks.</summary>
    public NvmeHealth? Nvme { get; init; }

    /// <summary>SSD heuristic via seek-penalty probe, engine verdict.</summary>
    public required bool? IsSsd { get; init; }

    /// <summary>TRIM state as reported (report-only — the engine cannot toggle it, gap G-15).</summary>
    public required TrimState Trim { get; init; }

    /// <summary>Engine warnings verbatim-classified (failing attribute / unstable media / critical bits).</summary>
    public required IReadOnlyList<HealthWarning> Warnings { get; init; }
}

public sealed record SmartAttribute
{
    /// <summary>Attribute ID (hex as engine prints).</summary>
    public required int Id { get; init; }

    public required string Name { get; init; }
    public required int Value { get; init; }
    public required int Worst { get; init; }
    public required long Raw { get; init; }
}

public sealed record SmartAggregates
{
    public required int Reallocated { get; init; }
    public required int Pending { get; init; }
    public required int Uncorrectable { get; init; }
    public required long PowerOnHours { get; init; }
    public required int TemperatureC { get; init; }
}

public sealed record NvmeHealth
{
    public required int TemperatureC { get; init; }
    public required double SparePercent { get; init; }
    public required double SpareThresholdPercent { get; init; }
    public required double UsedPercent { get; init; }
    public required long PowerOnHours { get; init; }
    public required long UnsafeShutdowns { get; init; }
    public required long MediaErrors { get; init; }
}

public enum TrimState
{
    Enabled,
    Disabled,
    Unknown,
}

public enum HealthWarningKind
{
    FailingAttribute,
    UnstableMedia,
    CriticalWarningBits,
    SmartUnavailable,
}

public sealed record HealthWarning(HealthWarningKind Kind, string EngineText);

/// <summary>Driver/transport status for the shell + About + scan preflight (TRANSPORT-CONTRACT §9).</summary>
public sealed record DriverStatus
{
    public required DriverState State { get; init; }

    /// <summary>Wire version when queryable, e.g. "1.0". Null when not reachable.</summary>
    public string? WireVersion { get; init; }

    /// <summary>Windows error code from the presence probe (diagnostics).</summary>
    public int? LastError { get; init; }

    /// <summary>True when the driver advertises the session engine (Flags bit).</summary>
    public bool? SessionsSupported { get; init; }

    /// <summary>MaxDisks from GET_INFO when available.</summary>
    public int? MaxDisks { get; init; }

    /// <summary>Human-readable truth for the status bar (localized at the view layer).</summary>
    public string TransportLabel => State switch
    {
        DriverState.LoadedOk => "RDA driver path",
        _ => "Direct-access mode",
    };
}

public enum DriverState
{
    /// <summary>Not installed (CreateFile GLE 2, no SCM service).</summary>
    NotInstalled,

    /// <summary>Service exists but is not running.</summary>
    InstalledNotLoaded,

    /// <summary>Present but access denied (SDDL — needs elevation).</summary>
    Blocked,

    /// <summary>Reachable but wrong/ancient version (GET_INFO/GET_VERSION fail).</summary>
    WrongVersion,

    /// <summary>Open + GET_INFO + GET_VERSION all good.</summary>
    LoadedOk,

    /// <summary>Probe could not run (non-Windows host in tests).</summary>
    Unknown,
}
