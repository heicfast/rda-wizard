// Recovery-domain components: DriveCard (home tile), SearchField (clearable search),
// Breadcrumb, RecoveryQueueRow, SessionBanner (continue-last-scanning strip).
// DriveCard spec (DESIGN-SYSTEM §8.2): drive icon 32 → name+letter BodyStrong →
// file-system Caption → capacity bar 156×4 r2 + "31.15 GB / 149.45 GB" tabular Caption
// → badges row (OS/USB/SSD 22×18, TRIM pill, BitLocker overlay). Selection = blue
// border + checkmark (atlas §5).
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RdaWizard.Controls;

/// <summary>Drive/media location card for the home grid.</summary>
public class DriveCard : RadioButton
{
    static DriveCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DriveCard),
            new FrameworkPropertyMetadata(typeof(DriveCard)));
    }

    public static readonly DependencyProperty DriveNameProperty =
        DependencyProperty.Register(nameof(DriveName), typeof(string), typeof(DriveCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LetterProperty =
        DependencyProperty.Register(nameof(Letter), typeof(string), typeof(DriveCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty FileSystemProperty =
        DependencyProperty.Register(nameof(FileSystem), typeof(string), typeof(DriveCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty UsedBytesProperty =
        DependencyProperty.Register(nameof(UsedBytes), typeof(double), typeof(DriveCard),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TotalBytesProperty =
        DependencyProperty.Register(nameof(TotalBytes), typeof(double), typeof(DriveCard),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CapacityTextProperty =
        DependencyProperty.Register(nameof(CapacityText), typeof(string), typeof(DriveCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(Geometry), typeof(DriveCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShowTrimChipProperty =
        DependencyProperty.Register(nameof(ShowTrimChip), typeof(bool), typeof(DriveCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowSsdBadgeProperty =
        DependencyProperty.Register(nameof(ShowSsdBadge), typeof(bool), typeof(DriveCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowOsBadgeProperty =
        DependencyProperty.Register(nameof(ShowOsBadge), typeof(bool), typeof(DriveCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowUsbBadgeProperty =
        DependencyProperty.Register(nameof(ShowUsbBadge), typeof(bool), typeof(DriveCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsLockedProperty =
        DependencyProperty.Register(nameof(IsLocked), typeof(bool), typeof(DriveCard),
            new PropertyMetadata(false));

    public string DriveName
    {
        get => (string)GetValue(DriveNameProperty);
        set => SetValue(DriveNameProperty, value);
    }

    public string Letter
    {
        get => (string)GetValue(LetterProperty);
        set => SetValue(LetterProperty, value);
    }

    public string FileSystem
    {
        get => (string)GetValue(FileSystemProperty);
        set => SetValue(FileSystemProperty, value);
    }

    public double UsedBytes
    {
        get => (double)GetValue(UsedBytesProperty);
        set => SetValue(UsedBytesProperty, value);
    }

    public double TotalBytes
    {
        get => (double)GetValue(TotalBytesProperty);
        set => SetValue(TotalBytesProperty, value);
    }

    public string CapacityText
    {
        get => (string)GetValue(CapacityTextProperty);
        set => SetValue(CapacityTextProperty, value);
    }

    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public bool ShowTrimChip
    {
        get => (bool)GetValue(ShowTrimChipProperty);
        set => SetValue(ShowTrimChipProperty, value);
    }

    public bool ShowSsdBadge
    {
        get => (bool)GetValue(ShowSsdBadgeProperty);
        set => SetValue(ShowSsdBadgeProperty, value);
    }

    public bool ShowOsBadge
    {
        get => (bool)GetValue(ShowOsBadgeProperty);
        set => SetValue(ShowOsBadgeProperty, value);
    }

    public bool ShowUsbBadge
    {
        get => (bool)GetValue(ShowUsbBadgeProperty);
        set => SetValue(ShowUsbBadgeProperty, value);
    }

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }
}

/// <summary>
/// SearchField — search box with embedded icon + clear button (QSS L4406 clear-icon
/// family, placeholder "Search files or folders" A35 §8). Height 32, r4.
/// </summary>
[TemplatePart(Name = "PART_ClearButton", Type = typeof(Button))]
public class SearchField : TextBox
{
    static SearchField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchField),
            new FrameworkPropertyMetadata(typeof(SearchField)));
    }

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchField),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HasTextProperty =
        DependencyProperty.Register(nameof(HasText), typeof(bool), typeof(SearchField),
            new PropertyMetadata(false));

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool HasText
    {
        get => (bool)GetValue(HasTextProperty);
        private set => SetValue(HasTextProperty, value);
    }

    private Button? _clearButton;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _clearButton = Template?.FindName("PART_ClearButton", this) as Button;
        if (_clearButton is not null)
        {
            _clearButton.Click += (_, _) =>
            {
                Clear();
                Focus();
            };
        }
        UpdateHasText();
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        UpdateHasText();
    }

    private void UpdateHasText() => HasText = Text.Length > 0;
}

/// <summary>
/// Breadcrumb — results-toolbar address strip (navigate_address_button family,
/// A34 §1; segments clickable, last = current, truncating middle with ellipsis).
/// </summary>
public class Breadcrumb : ItemsControl
{
    static Breadcrumb()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Breadcrumb),
            new FrameworkPropertyMetadata(typeof(Breadcrumb)));
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(Breadcrumb),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
}

/// <summary>
/// RecoveryQueueRow — destination/queue row: drive icon + name + capacity + fit-verdict
/// chip (Fits / "Needs 146.41 GB more", A35 §10) + selection.
/// </summary>
public class RecoveryQueueRow : RadioButton
{
    static RecoveryQueueRow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RecoveryQueueRow),
            new FrameworkPropertyMetadata(typeof(RecoveryQueueRow)));
    }

    public static readonly DependencyProperty DestinationNameProperty =
        DependencyProperty.Register(nameof(DestinationName), typeof(string), typeof(RecoveryQueueRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CapacityTextProperty =
        DependencyProperty.Register(nameof(CapacityText), typeof(string), typeof(RecoveryQueueRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VerdictTextProperty =
        DependencyProperty.Register(nameof(VerdictText), typeof(string), typeof(RecoveryQueueRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VerdictKindProperty =
        DependencyProperty.Register(nameof(VerdictKind), typeof(int), typeof(RecoveryQueueRow),
            new PropertyMetadata(0)); // 0 fits, 1 warning (needs more), 2 blocked (source)

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(Geometry), typeof(RecoveryQueueRow),
            new PropertyMetadata(null));

    public string DestinationName
    {
        get => (string)GetValue(DestinationNameProperty);
        set => SetValue(DestinationNameProperty, value);
    }

    public string CapacityText
    {
        get => (string)GetValue(CapacityTextProperty);
        set => SetValue(CapacityTextProperty, value);
    }

    public string VerdictText
    {
        get => (string)GetValue(VerdictTextProperty);
        set => SetValue(VerdictTextProperty, value);
    }

    public int VerdictKind
    {
        get => (int)GetValue(VerdictKindProperty);
        set => SetValue(VerdictKindProperty, value);
    }

    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
}

/// <summary>
/// SessionBanner — "Continue last scanning" strip (A07 §2: scan time · scan files ·
/// disk metadata + Load action). Collapsible; not dismissible while a session exists.
/// </summary>
public class SessionBanner : Banner
{
    static SessionBanner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SessionBanner),
            new FrameworkPropertyMetadata(typeof(SessionBanner)));
    }

    public static readonly DependencyProperty SessionTimeProperty =
        DependencyProperty.Register(nameof(SessionTime), typeof(string), typeof(SessionBanner),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SessionFilesProperty =
        DependencyProperty.Register(nameof(SessionFiles), typeof(string), typeof(SessionBanner),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SessionDiskProperty =
        DependencyProperty.Register(nameof(SessionDisk), typeof(string), typeof(SessionBanner),
            new PropertyMetadata(string.Empty));

    public string SessionTime
    {
        get => (string)GetValue(SessionTimeProperty);
        set => SetValue(SessionTimeProperty, value);
    }

    public string SessionFiles
    {
        get => (string)GetValue(SessionFilesProperty);
        set => SetValue(SessionFilesProperty, value);
    }

    public string SessionDisk
    {
        get => (string)GetValue(SessionDiskProperty);
        set => SetValue(SessionDiskProperty, value);
    }
}
