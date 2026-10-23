// ToggleSwitch — on/off switch control (Settings: theme picker, file-repair enable,
// hide-system-files toggles).
// Spec: DESIGN-SYSTEM §8 (inputs); track 40×20, thumb 16, brand fill when on,
// SurfaceAlt when off; states: on/off/hover/pressed/disabled/focus(visual).
// A11y: acts as a toggle (UIA Toggle pattern via IsChecked), AutomationProperties.Name
// bound from Header/Content.
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace RdaWizard.Controls;

public class ToggleSwitch : Control
{
    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        FocusableProperty.OverrideMetadata(typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(true));
        KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(true));
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault |
                FrameworkPropertyMetadataOptions.Journal,
                OnIsCheckedChanged));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(ToggleSwitch),
            new PropertyMetadata(string.Empty, OnHeaderChanged));

    public static readonly DependencyProperty OnTextProperty =
        DependencyProperty.Register(nameof(OnText), typeof(string), typeof(ToggleSwitch),
            new PropertyMetadata("On"));

    public static readonly DependencyProperty OffTextProperty =
        DependencyProperty.Register(nameof(OffText), typeof(string), typeof(ToggleSwitch),
            new PropertyMetadata("Off"));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string OnText
    {
        get => (string)GetValue(OnTextProperty);
        set => SetValue(OnTextProperty, value);
    }

    public string OffText
    {
        get => (string)GetValue(OffTextProperty);
        set => SetValue(OffTextProperty, value);
    }

    public static readonly RoutedEvent ToggledEvent = EventManager.RegisterRoutedEvent(
        nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleSwitch));

    public event RoutedEventHandler Toggled
    {
        add => AddHandler(ToggledEvent, value);
        remove => RemoveHandler(ToggledEvent, value);
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ToggleSwitch)d;
        c.RaiseEvent(new RoutedEventArgs(ToggledEvent, c));
        // Announce the state change to assistive tech.
        if (!string.IsNullOrEmpty(c.Header))
        {
            AutomationProperties.SetName(c, $"{c.Header}, {((bool)e.NewValue ? c.OnText : c.OffText)}");
        }
    }

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ToggleSwitch)d;
        AutomationProperties.SetName(c, $"{e.NewValue}, {(c.IsChecked ? c.OnText : c.OffText)}");
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        // Whole track is the hit target; toggle on press for immediacy (press-state renders).
        if (IsEnabled)
        {
            IsChecked = !IsChecked;
        }
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && IsEnabled)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }
    }
}
