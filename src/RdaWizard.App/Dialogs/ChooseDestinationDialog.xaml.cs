using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using RdaWizard.App.Services;
using RdaWizard.Core.Models;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Dialogs;

/// <summary>
/// og ChooseDestinationPathDialog: volume rows (with the OS capacity pass
/// and the source-disk warning icon), "Choose folder..." row, and Save —
/// disabled until a destination is selected. Returns the chosen path via
/// <see cref="SelectedPath"/> when <see cref="Window.ShowDialog"/> returns
/// true.
/// </summary>
public partial class ChooseDestinationDialog : Window, INotifyPropertyChanged
{
    /// <summary>One destination row (og ChooseDestinationPathItem).</summary>
    public sealed record DestinationItem(char Letter, string Label, long FreeBytes, long SizeBytes, bool IsSourceDisk)
    {
        public string Name => string.IsNullOrWhiteSpace(Label)
            ? $"{Letter}:"
            : $"{Label} ({Letter}:)";

        public string CapacityText => $"{FreeBytes >> 30} GB free of {SizeBytes >> 30} GB";
    }

    private readonly int _sourceDiskIndex;

    public ObservableCollection<DestinationItem> Destinations { get; } = [];

    public string RecoverableText { get; }

    public DestinationItem? SelectedDestination
    {
        get => _selectedDestination;
        set
        {
            _selectedDestination = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDestination)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelection)));
        }
    }

    private DestinationItem? _selectedDestination;

    public bool HasSelection => SelectedDestination is not null || _folderPick is not null;

    /// <summary>The chosen destination directory (volume root or picked
    /// folder); null when cancelled.</summary>
    public string? SelectedPath { get; private set; }

    private string? _folderPick;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChooseDestinationDialog(IReadOnlyList<VolumeInfo> volumes, int sourceDiskIndex, long neededBytes, long fileCount)
    {
        _sourceDiskIndex = sourceDiskIndex;
        RecoverableText = $"You have {fileCount:N0} file(s) ({neededBytes / (1024.0 * 1024 * 1024):0.##} GB) to recover.";

        foreach (var volume in volumes.Where(v => v.Letter is not null).OrderBy(v => v.Letter))
        {
            long free = 0;
            try
            {
                var drive = new DriveInfo(volume.Letter!.Value.ToString());
                if (drive.IsReady)
                {
                    free = drive.AvailableFreeSpace;
                }
            }
            catch (ArgumentException)
            {
                // defensive: unmounted letters were filtered above
            }

            Destinations.Add(new DestinationItem(volume.Letter!.Value, volume.Label, free, volume.SizeBytes, volume.OwningDiskIndex == sourceDiskIndex));
        }

        InitializeComponent();
        DataContext = this;
    }

    private void OnChooseFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose a destination folder to save the recovered files",
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _folderPick = dialog.FolderName;
        SelectedDestination = null; // folder pick is the selection now
        SelectedPath = _folderPick;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelection)));
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (SelectedDestination is { } volume)
        {
            SelectedPath = volume.Letter + ":\\Recovered files";
        }
        else if (_folderPick is not null)
        {
            SelectedPath = _folderPick;
        }
        else
        {
            return;
        }

        DialogResult = true;
    }
}
