namespace RdaWizard.Core.Models;

/// <summary>Physical disk as reported by the engine (rrecov list).</summary>
public sealed record DiskInfo
{
    /// <summary>Engine disk index (PhysicalDriveN). Stable within one enumeration.</summary>
    public required int Index { get; init; }

    /// <summary>Total size in bytes (engine reports MiB; parser scales).</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Model string, trimmed; may be empty for some USB bridges.</summary>
    public required string Model { get; init; } = string.Empty;

    /// <summary>Serial string, trimmed; may be empty.</summary>
    public required string Serial { get; init; } = string.Empty;

    /// <summary>removable vs fixed (engine's classification).</summary>
    public required bool IsRemovable { get; init; }

    /// <summary>Partition table kind as the engine reports it.</summary>
    public required PartitionTableKind Table { get; init; }

    /// <summary>Volumes the engine associates with this disk (list output nests them).</summary>
    public IReadOnlyList<VolumeInfo> Volumes { get; init; } = Array.Empty<VolumeInfo>();

    /// <summary>Display name, e.g. "Samsung SSD 970 EVO 1TB" or "USB Drive".</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Model)
        ? $"{(IsRemovable ? "Removable disk" : "Disk")} {Index}"
        : Model;
}

public enum PartitionTableKind
{
    Gpt,
    Mbr,
    /// <summary>No table observed — whole-disk / lost-partition territory.</summary>
    None,
}

/// <summary>Logical volume (drive letter + fs + label).</summary>
public sealed record VolumeInfo
{
    /// <summary>Drive letter ('C'), or null for unmounted volumes if the engine ever reports them.</summary>
    public required char? Letter { get; init; }

    /// <summary>Filesystem label exactly as the engine prints it (e.g. NTFS, FAT32, exFAT, RAW).</summary>
    public required string Filesystem { get; init; } = string.Empty;

    /// <summary>Volume label, may be empty.</summary>
    public required string Label { get; init; } = string.Empty;

    /// <summary>Volume size in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Used space in bytes from the OS capacity pass (the og's
    /// WMI/mountmgr equivalent — DriveInfo), null when the OS reports none
    /// (unmounted/BitLocker volumes). Not engine data: the engine's list
    /// line carries totals only.</summary>
    public long? UsedBytes { get; init; }

    /// <summary>Engine flagged the volume BitLocker-locked (auto-unlock is engine-side).</summary>
    public required bool IsBitLocker { get; init; }

    /// <summary>Owning physical disk (resolved from list nesting / volume extents).</summary>
    public required int OwningDiskIndex { get; init; }

    /// <summary>Tile-facing name in the og format: label + letter, e.g.
    /// "Windows(C:)" / "Data(D:)"; unmounted volumes degrade to the
    /// filesystem name per the og's lost-volume tiles.</summary>
    public string TileName
    {
        get
        {
            if (Letter is { } letter)
            {
                var label = string.IsNullOrWhiteSpace(Label) ? Filesystem : Label;
                return $"{label}({letter}:)";
            }
            return $"{Filesystem} volume";
        }
    }

    public string DisplayName => Letter is { } letter
        ? $"{letter}: {(string.IsNullOrWhiteSpace(Label) ? Filesystem : Label)}"
        : $"Unmounted ({Filesystem})";
}
