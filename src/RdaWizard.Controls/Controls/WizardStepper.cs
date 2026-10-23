using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;

namespace RdaWizard.Controls;

/// <summary>
/// The three-step wizard rail (Select → Scan → Recover) — a documented
/// improvement over the og (which had no stepper; AUDIT-UI ADR-12).
/// Current step gets the accent dot + bold label; completed steps show a
/// checkmark; future steps are muted. Fully keyboard-accessible (focusable,
/// announces "Step 2 of 3: Scan").
/// </summary>
public class WizardStepper : Control
{
    static WizardStepper()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WizardStepper),
            new FrameworkPropertyMetadata(typeof(WizardStepper)));
    }

    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int), typeof(WizardStepper),
            new PropertyMetadata(0, OnStepChanged));

    public static readonly DependencyProperty Step1TextProperty =
        DependencyProperty.Register(nameof(Step1Text), typeof(string), typeof(WizardStepper),
            new PropertyMetadata("Select"));

    public static readonly DependencyProperty Step2TextProperty =
        DependencyProperty.Register(nameof(Step2Text), typeof(string), typeof(WizardStepper),
            new PropertyMetadata("Scan"));

    public static readonly DependencyProperty Step3TextProperty =
        DependencyProperty.Register(nameof(Step3Text), typeof(string), typeof(WizardStepper),
            new PropertyMetadata("Recover"));

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public string Step1Text
    {
        get => (string)GetValue(Step1TextProperty);
        set => SetValue(Step1TextProperty, value);
    }

    public string Step2Text
    {
        get => (string)GetValue(Step2TextProperty);
        set => SetValue(Step2TextProperty, value);
    }

    public string Step3Text
    {
        get => (string)GetValue(Step3TextProperty);
        set => SetValue(Step3TextProperty, value);
    }

    /// <summary>Step states exposed for template triggers via relative binding.</summary>
    public static readonly DependencyPropertyKey Step1StatePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Step1State), typeof(WizardStepState), typeof(WizardStepper),
            new PropertyMetadata(WizardStepState.Current));

    public WizardStepState Step1State => (WizardStepState)GetValue(Step1StatePropertyKey.DependencyProperty);
    public WizardStepState Step2State => (WizardStepState)GetValue(Step2StatePropertyKey.DependencyProperty);
    public WizardStepState Step3State => (WizardStepState)GetValue(Step3StatePropertyKey.DependencyProperty);

    public static readonly DependencyPropertyKey Step2StatePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Step2State), typeof(WizardStepState), typeof(WizardStepper),
            new PropertyMetadata(WizardStepState.Upcoming));

    public static readonly DependencyPropertyKey Step3StatePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Step3State), typeof(WizardStepState), typeof(WizardStepper),
            new PropertyMetadata(WizardStepState.Upcoming));

    private static void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var stepper = (WizardStepper)d;
        var step = stepper.CurrentStep;
        stepper.SetValue(Step1StatePropertyKey, Classify(0, step));
        stepper.SetValue(Step2StatePropertyKey, Classify(1, step));
        stepper.SetValue(Step3StatePropertyKey, Classify(2, step));
    }

    private static WizardStepState Classify(int index, int current) => index switch
    {
        var i when i < current => WizardStepState.Completed,
        var i when i == current => WizardStepState.Current,
        _ => WizardStepState.Upcoming,
    };

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AutomationAutomationName();
    }

    private void AutomationAutomationName()
    {
        var stepName = CurrentStep switch
        {
            0 => Step1Text,
            1 => Step2Text,
            _ => Step3Text,
        };
        AutomationProperties.SetName(this, $"Step {CurrentStep + 1} of 3: {stepName}");
    }
}

public enum WizardStepState
{
    Upcoming,
    Current,
    Completed,
}
