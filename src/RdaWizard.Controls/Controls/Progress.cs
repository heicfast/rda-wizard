// Selection & progress components: Segmented control, ProgressSurface (linear
// determinate/indeterminate/staged), CircularGauge (scan % ring), FileTypeFilterChip.
// Evidence: circular gauge (atlas §6: blue ring, % center, center (48,687) on runner);
// staged progress (scan phases quick→deep); segmented = classify strip (A34 §1);
// filter chips (QSS filter_item family, A31).
using System.Collections;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace RdaWizard.Controls;

/// <summary>
/// Segmented control — mutually exclusive compact selector (list/icon view toggle,
/// Local/Cloud tabs in destination picker, density switch).
/// ItemsSource (strings or SegmentedItem), SelectedIndex; keyboard: arrows + Home/End.
/// </summary>
public class SegmentedItem
{
    public string Label { get; init; } = string.Empty;
    public object? Tag { get; init; }
}

public class Segmented : Control
{
    static Segmented()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Segmented),
            new FrameworkPropertyMetadata(typeof(Segmented)));
        FocusableProperty.OverrideMetadata(typeof(Segmented),
            new FrameworkPropertyMetadata(false));
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(Segmented),
            new PropertyMetadata(null, (d, _) => ((Segmented)d).Rebuild()));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(Segmented),
            new FrameworkPropertyMetadata(-1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, e) => ((Segmented)d).OnSelectionChanged((int)e.NewValue)));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Segmented));

    public event RoutedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    private WrapPanel? _panel;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _panel = Template?.FindName("PART_Panel", this) as WrapPanel;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_panel is null)
        {
            return;
        }
        _panel.Children.Clear();
        if (ItemsSource is null)
        {
            return;
        }
        var index = 0;
        foreach (var item in ItemsSource)
        {
            var label = item switch
            {
                SegmentedItem si => si.Label,
                string s => s,
                _ => item?.ToString() ?? string.Empty,
            };
            var buttonIndex = index;
            var b = new RadioButton
            {
                Content = label,
                Style = (Style)TryFindResource(new ComponentResourceKey(typeof(Segmented), "SegmentedItemStyle"))
                        ?? (Style)TryFindResource("Segmented.ItemStyle"),
                GroupName = "seg_" + GetHashCode(),
                IsChecked = buttonIndex == SelectedIndex,
                Focusable = true,
                MinWidth = 56,
                Margin = new Thickness(0),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand,
            };
            System.Windows.Automation.AutomationProperties.SetName(b, label);
            b.Checked += (_, _) =>
            {
                if (SelectedIndex != buttonIndex)
                {
                    SelectedIndex = buttonIndex;
                }
            };
            _panel.Children.Add(b);
            index++;
        }
    }

    private void OnSelectionChanged(int newIndex)
    {
        if (_panel is null || newIndex < 0 || newIndex >= _panel.Children.Count)
        {
            return;
        }
        if (_panel.Children[newIndex] is RadioButton rb)
        {
            rb.IsChecked = true;
        }
        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent, this));
    }
}

/// <summary>
/// ProgressSurface — the scan/recovery progress component.
/// Modes: Determinate (0..100, animated smoothing 300ms), Indeterminate (shimmer band,
/// 1600ms loop — replaces DRW PNG frame strips), Staged (segmented phase chips:
/// Quick scan → Deep scan → Organizing, per DYN-scan §7).
/// </summary>
public class ProgressSurface : Control
{
    static ProgressSurface()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressSurface),
            new FrameworkPropertyMetadata(typeof(ProgressSurface)));
    }

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(ProgressSurface),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressSurface),
            new FrameworkPropertyMetadata(0.0,
                (d, e) => ((ProgressSurface)d).OnValueChanged((double)e.NewValue)));

    public static readonly DependencyProperty StageLabelProperty =
        DependencyProperty.Register(nameof(StageLabel), typeof(string), typeof(ProgressSurface),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailTextProperty =
        DependencyProperty.Register(nameof(DetailText), typeof(string), typeof(ProgressSurface),
            new PropertyMetadata(string.Empty));

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string StageLabel
    {
        get => (string)GetValue(StageLabelProperty);
        set => SetValue(StageLabelProperty, value);
    }

    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    private Rectangle? _fill;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _fill = Template?.FindName("PART_Fill", this) as Rectangle;
        ApplyFillWidth(false);
    }

    private void OnValueChanged(double newValue)
    {
        // Determinate smoothing: sector-counter jumps must not strobe (DYN-scan §7).
        ApplyFillWidth(SystemParameters.ClientAreaAnimation);
    }

    private void ApplyFillWidth(bool animate)
    {
        if (_fill is null || IsIndeterminate)
        {
            return;
        }
        var clamped = Math.Clamp(Value, 0, 100) / 100.0;
        if (ActualWidth <= 0 && double.IsNaN(Width))
        {
            // Defer until measured (first layout pass).
            Dispatcher.BeginInvoke(() => ApplyFillWidth(animate), System.Windows.Threading.DispatcherPriority.Loaded);
            return;
        }
        var target = Math.Max(0, Math.Min(1, clamped)) * (ActualWidth > 0 ? ActualWidth : Width);
        if (animate)
        {
            var duration = TimeSpan.FromMilliseconds(300);
            _fill.BeginAnimation(WidthProperty, new DoubleAnimation(target, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            });
        }
        else
        {
            _fill.BeginAnimation(WidthProperty, null);
            _fill.Width = target;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        ApplyFillWidth(false);
    }
}

/// <summary>
/// CircularGauge — the scan-progress ring (atlas §6: ring x16–79 on 1024 runner,
/// blue arc, large % numeral center). Also used for "Recovery Chances" (A35 §11)
/// and HealthGauge-style rings.
/// </summary>
public class CircularGauge : Control
{
    static CircularGauge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CircularGauge),
            new FrameworkPropertyMetadata(typeof(CircularGauge)));
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularGauge),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((CircularGauge)d).OnValueChanged((double)e.NewValue)));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(CircularGauge),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubLabelProperty =
        DependencyProperty.Register(nameof(SubLabel), typeof(string), typeof(CircularGauge),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPausedProperty =
        DependencyProperty.Register(nameof(IsPaused), typeof(bool), typeof(CircularGauge),
            new PropertyMetadata(false));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string SubLabel
    {
        get => (string)GetValue(SubLabelProperty);
        set => SetValue(SubLabelProperty, value);
    }

    public bool IsPaused
    {
        get => (bool)GetValue(IsPausedProperty);
        set => SetValue(IsPausedProperty, value);
    }

    private Arc? _arc;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _arc = Template?.FindName("PART_Arc", this) as Arc;
        UpdateArc(0);
    }

    private void OnValueChanged(double newValue) => UpdateArc(SystemParameters.ClientAreaAnimation ? 300 : 0);

    private void UpdateArc(double animateMs)
    {
        if (_arc is null)
        {
            return;
        }
        _arc.Sweep = Math.Clamp(Value, 0, 100) / 100.0 * 359.9;
        if (animateMs > 0)
        {
            // The Arc animates via its own Sweep property re-render; for smooth
            // count-up, animate Value at the call-site (VM already ticks counters).
        }
        AutomationProperties.SetName(this,
            $"{Label} {Math.Round(Value)} percent{(IsPaused ? ", paused" : string.Empty)}");
    }
}

/// <summary>
/// FileTypeFilterChip — toggleable category chip with icon + label + live count
/// (QSS filter_item family, A31 §1; per-category "Select All (N)" with counts, A35 §9).
/// States: normal/hover/pressed/selected/disabled; count uses tabular numerals.
/// </summary>
public class FileTypeFilterChip : ToggleButton
{
    static FileTypeFilterChip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FileTypeFilterChip),
            new FrameworkPropertyMetadata(typeof(FileTypeFilterChip)));
    }

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(nameof(Count), typeof(long), typeof(FileTypeFilterChip),
            new PropertyMetadata(0L));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(Geometry), typeof(FileTypeFilterChip),
            new PropertyMetadata(null));

    public long Count
    {
        get => (long)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
}
