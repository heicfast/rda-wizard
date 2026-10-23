// Icon — renders a named icon geometry from the design system (Icons.xaml) at a token
// size with the correct stroke/fill mode. Chrome/action icons are STROKE (1.5px, round
// caps/joins); device/file tiles are FILLED. Theme-aware foreground via DynamicResource.
// A11y: decorative by default (empty AutomationProperties.Name); set Name to make it
// informative. Sizes snap to token set 16/20/24/32/48.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RdaWizard.Controls;

public enum IconKind
{
    /// <summary>Stroke-rendered chrome/action icon (24-grid, 1.5px stroke).</summary>
    Chrome,
    /// <summary>Filled tile/device icon (20-grid).</summary>
    Solid,
    /// <summary>Large empty-state illustration art (24..64-grid).</summary>
    Art,
}

public class Icon : Control
{
    private const double DefaultChromeStroke = 1.5;

    static Icon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Icon),
            new FrameworkPropertyMetadata(typeof(Icon)));
        FocusableProperty.OverrideMetadata(typeof(Icon),
            new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(Icon),
            new FrameworkPropertyMetadata(false));
    }

    public static readonly DependencyProperty IconNameProperty =
        DependencyProperty.Register(nameof(IconName), typeof(string), typeof(Icon),
            new PropertyMetadata(string.Empty, OnIconChanged));

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(IconKind), typeof(Icon),
            new PropertyMetadata(IconKind.Chrome, OnIconChanged));

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(Icon),
            new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnIconChanged));

    public static readonly DependencyProperty IconBrushProperty =
        DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(Icon),
            new PropertyMetadata(null, OnIconChanged));

    public string IconName
    {
        get => (string)GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    public IconKind IconKind
    {
        get => (IconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>Override foreground; null = inherit Foreground (theme Text/Secondary by default style).</summary>
    public Brush? IconBrush
    {
        get => (Brush?)GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    private Path? _path;
    private bool _templateApplied;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _path = Template?.FindName("PART_Path", this) as Path;
        _templateApplied = true;
        Rebuild();
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Icon)d).Rebuild();
    }

    private void Rebuild()
    {
        if (!_templateApplied || _path is null)
        {
            return;
        }
        var geometry = ResolveGeometry();
        _path.Data = geometry;
        if (IconKind == IconKind.Chrome)
        {
            _path.Stroke = IconBrush ?? Foreground;
            _path.StrokeThickness = ChromeStrokeFor(IconSize);
            _path.Fill = null;
        }
        else
        {
            _path.Fill = IconBrush ?? Foreground;
            _path.Stroke = null;
            _path.StrokeThickness = 0;
        }
        // Optical sizing: 16/20/24 stroke icons keep the same 24-grid geometry,
        // scaled uniformly — stroke weight scales with it (crisp at 100–200% DPI
        // via SnapsToDevicePixels on the host).
        Width = IconSize;
        Height = IconSize;
    }

    private Geometry? ResolveGeometry()
    {
        if (string.IsNullOrEmpty(IconName))
        {
            return null;
        }
        var key = IconName.StartsWith("Icon.", StringComparison.Ordinal) ? IconName : "Icon." + IconName;
        return TryFindResource(key) as Geometry;
    }

    private static double ChromeStrokeFor(double size) => size switch
    {
        <= 16 => 1.75,   // optical compensation: thin strokes vanish when downscaled
        <= 20 => 1.6,
        <= 24 => 1.5,
        <= 32 => 1.25,   // upscale thins relatively
        _ => 1.0,
    };
}
