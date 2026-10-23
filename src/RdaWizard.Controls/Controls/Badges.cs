using System.Windows;
using System.Windows.Controls;

namespace RdaWizard.Controls;

/// <summary>
/// Confidence badge — displays the ENGINE's grade (never re-graded):
/// Excellent / PartialRisk / Overwritten / MetadataOnly / Unknown with the
/// exact grade as tooltip text. Colors from the Confidence.* tokens.
/// </summary>
public class ConfidenceBadge : Control
{
    static ConfidenceBadge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ConfidenceBadge),
            new FrameworkPropertyMetadata(typeof(ConfidenceBadge)));
    }

    public static readonly DependencyProperty GradeProperty =
        DependencyProperty.Register(nameof(Grade), typeof(int), typeof(ConfidenceBadge),
            new PropertyMetadata(0, OnGradeChanged));

    /// <summary>Engine confidence grade as the raw numeric enum (0..4).</summary>
    public int Grade
    {
        get => (int)GetValue(GradeProperty);
        set => SetValue(GradeProperty, value);
    }

    public static readonly DependencyPropertyKey LabelPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Label), typeof(string), typeof(ConfidenceBadge),
            new PropertyMetadata("Unknown"));

    public string Label => (string)GetValue(LabelPropertyKey.DependencyProperty);

    private static void OnGradeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var badge = (ConfidenceBadge)d;
        var grade = badge.Grade;
        badge.SetValue(LabelPropertyKey, grade switch
        {
            1 => "Excellent",
            2 => "Partial risk",
            3 => "Overwritten",
            4 => "Metadata only",
            _ => "Unknown",
        });
        AutomationProperties.SetName(badge, $"Recovery confidence: {badge.Label}");
    }
}

/// <summary>
/// Drive capacity bar with optional health tint (Good/Caution/Bad/Unknown
/// bands per DESIGN-SYSTEM health tokens). Used on DriveCards and the
/// health panel gauge.
/// </summary>
public class HealthGauge : Control
{
    static HealthGauge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HealthGauge),
            new FrameworkPropertyMetadata(typeof(HealthGauge)));
    }

    public static readonly DependencyProperty BandProperty =
        DependencyProperty.Register(nameof(Band), typeof(int), typeof(HealthGauge),
            new PropertyMetadata(3));

    /// <summary>0 Good / 1 Caution / 2 Bad / 3 Unknown.</summary>
    public int Band
    {
        get => (int)GetValue(BandProperty);
        set => SetValue(BandProperty, value);
    }

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(HealthGauge),
            new PropertyMetadata(string.Empty));

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }
}

/// <summary>
/// State surfaces for pages: EmptyState (no content: first-run, no devices,
/// no results) and ErrorState (diagnostic + retry) — every page must
/// handle loading/empty/error (standing rule).
/// </summary>
public class StateSurface : Control
{
    static StateSurface()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StateSurface),
            new FrameworkPropertyMetadata(typeof(StateSurface)));
    }

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(StateSurface),
            new PropertyMetadata("\uE7B8")); // Search-ish default

    public static readonly DependencyProperty HeadlineProperty =
        DependencyProperty.Register(nameof(Headline), typeof(string), typeof(StateSurface),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(nameof(Detail), typeof(string), typeof(StateSurface),
            new PropertyMetadata(string.Empty));

    /// <summary>True = error styling (danger accent); false = neutral empty styling.</summary>
    public static readonly DependencyProperty IsErrorProperty =
        DependencyProperty.Register(nameof(IsError), typeof(bool), typeof(StateSurface),
            new PropertyMetadata(false));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public bool IsError
    {
        get => (bool)GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }
}
