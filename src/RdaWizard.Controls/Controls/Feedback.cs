// Feedback controls: Banner (persistent severity strip) and Toast (transient notice).
// Banner spec (DESIGN-SYSTEM §8.15): full-width 48px strip, r8, 4px severity accent bar
// left, icon 20 + Title BodyStrong + body Caption + action right; dismissible except
// critical. DRW evidence: session-resume banner (A07 §2), NAS tips (A08 §5),
// insufficient-space callout (A35 §10), paused strip (A35 §6).
// Toast spec: 360 max width, r8, elevated bg, auto-dismiss (default 5s), slide+fade
// entrance; DRW evidence: "AI Recommend available." toast (A35/P37).
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RdaWizard.Controls;

public enum BannerSeverity
{
    Info,
    Success,
    Warning,
    Danger,
}

public class Banner : Control
{
    static Banner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Banner),
            new FrameworkPropertyMetadata(typeof(Banner)));
        FocusableProperty.OverrideMetadata(typeof(Banner),
            new FrameworkPropertyMetadata(false));
    }

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(BannerSeverity), typeof(Banner),
            new PropertyMetadata(BannerSeverity.Info));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(Banner),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(Banner),
            new PropertyMetadata(string.Empty, (d, _) => ((Banner)d).UpdateAutomationName()));

    public static readonly DependencyProperty ActionLabelProperty =
        DependencyProperty.Register(nameof(ActionLabel), typeof(string), typeof(Banner),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsDismissibleProperty =
        DependencyProperty.Register(nameof(IsDismissible), typeof(bool), typeof(Banner),
            new PropertyMetadata(true));

    public BannerSeverity Severity
    {
        get => (BannerSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ActionLabel
    {
        get => (string)GetValue(ActionLabelProperty);
        set => SetValue(ActionLabelProperty, value);
    }

    public bool IsDismissible
    {
        get => (bool)GetValue(IsDismissibleProperty);
        set => SetValue(IsDismissibleProperty, value);
    }

    public static readonly RoutedEvent ActionInvokedEvent = EventManager.RegisterRoutedEvent(
        nameof(ActionInvoked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Banner));

    public event RoutedEventHandler ActionInvoked
    {
        add => AddHandler(ActionInvokedEvent, value);
        remove => RemoveHandler(ActionInvokedEvent, value);
    }

    public static readonly RoutedEvent DismissedEvent = EventManager.RegisterRoutedEvent(
        nameof(Dismissed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Banner));

    public event RoutedEventHandler Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    private void UpdateAutomationName() =>
        AutomationProperties.SetName(this, $"Banner {Severity}: {Title}. {Message}");

    // Template part contract: PART_ActionButton (optional), PART_DismissButton (optional).
    private Button? _actionButton;
    private Button? _dismissButton;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _actionButton = Template?.FindName("PART_ActionButton", this) as Button;
        if (_actionButton is not null)
        {
            _actionButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(ActionInvokedEvent, this));
        }
        _dismissButton = Template?.FindName("PART_DismissButton", this) as Button;
        if (_dismissButton is not null)
        {
            _dismissButton.Click += (_, _) =>
            {
                Visibility = Visibility.Collapsed;
                RaiseEvent(new RoutedEventArgs(DismissedEvent, this));
            };
        }
        UpdateAutomationName();
    }
}

public enum ToastSeverity
{
    Info,
    Success,
    Warning,
    Danger,
}

/// <summary>
/// Transient toast notification (360px, elevated, auto-dismiss 5s default, hover pauses
/// the timer, reduced-motion = instant appearance). Host inside a panel anchored
/// bottom-center or bottom-right of the window.
/// </summary>
public class Toast : Control
{
    static Toast()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Toast),
            new FrameworkPropertyMetadata(typeof(Toast)));
        FocusableProperty.OverrideMetadata(typeof(Toast),
            new FrameworkPropertyMetadata(false));
    }

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(ToastSeverity), typeof(Toast),
            new PropertyMetadata(ToastSeverity.Info));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(Toast),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(Toast),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionLabelProperty =
        DependencyProperty.Register(nameof(ActionLabel), typeof(string), typeof(Toast),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(Toast),
            new PropertyMetadata(TimeSpan.FromSeconds(5)));

    public ToastSeverity Severity
    {
        get => (ToastSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ActionLabel
    {
        get => (string)GetValue(ActionLabelProperty);
        set => SetValue(ActionLabelProperty, value);
    }

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public static readonly RoutedEvent ActionInvokedEvent = EventManager.RegisterRoutedEvent(
        nameof(ActionInvoked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Toast));

    public event RoutedEventHandler ActionInvoked
    {
        add => AddHandler(ActionInvokedEvent, value);
        remove => RemoveHandler(ActionInvokedEvent, value);
    }

    public static readonly RoutedEvent ExpiredEvent = EventManager.RegisterRoutedEvent(
        nameof(Expired), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Toast));

    public event RoutedEventHandler Expired
    {
        add => AddHandler(ExpiredEvent, value);
        remove => RemoveHandler(ExpiredEvent, value);
    }

    private readonly DispatcherTimer? _timer;
    private Button? _actionButton;

    public Toast()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background);
        _timer.Tick += (_, _) => Expire();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _actionButton = Template?.FindName("PART_ActionButton", this) as Button;
        if (_actionButton is not null)
        {
            _actionButton.Click += (_, _) => RaiseEvent(new RoutedEventArgs(ActionInvokedEvent, this));
        }
        AutomationProperties.SetName(this, $"Toast {Severity}: {Title}. {Message}");
        StartLifecycle();
    }

    protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
    {
        _timer?.Stop(); // hover pauses auto-dismiss (reading courtesy)
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        if (_timer is not null && _timer.Interval != TimeSpan.Zero && Visibility == Visibility.Visible)
        {
            _timer.Start();
        }
        base.OnMouseLeave(e);
    }

    /// <summary>Begin entrance + the auto-dismiss countdown. Re-entrant.</summary>
    public void StartLifecycle()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            Visibility = Visibility.Visible; // reduced motion: instant
        }
        else
        {
            Visibility = Visibility.Visible;
            PlayEntrance();
        }
        if (Duration > TimeSpan.Zero && _timer is not null)
        {
            _timer.Interval = Duration;
            _timer.Stop();
            _timer.Start();
        }
    }

    private void PlayEntrance()
    {
        var duration = TimeSpan.FromMilliseconds(MotionDuration("Motion.ToastIn", 250));
        var fade = new DoubleAnimation(0, 1, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var slide = new ThicknessAnimation(new Thickness(0, 12, 0, 0), new Thickness(0), duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        BeginAnimation(OpacityProperty, fade);
        BeginAnimation(MarginProperty, slide);
    }

    private static double MotionDuration(string key, double fallback)
    {
        try
        {
            return Application.Current?.TryFindResource(key) is double d && d > 0 ? d : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private void Expire()
    {
        _timer?.Stop();
        if (SystemParameters.ClientAreaAnimation)
        {
            var duration = TimeSpan.FromMilliseconds(MotionDuration("Motion.ToastOut", 200));
            var fade = new DoubleAnimation(1, 0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
            fade.Completed += (_, _) => RaiseEvent(new RoutedEventArgs(ExpiredEvent, this));
            BeginAnimation(OpacityProperty, fade);
        }
        else
        {
            RaiseEvent(new RoutedEventArgs(ExpiredEvent, this));
        }
    }
}
