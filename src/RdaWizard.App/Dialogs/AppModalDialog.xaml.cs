using System.Windows;
using System.Windows.Input;
using RdaWizard.App.Services;

namespace RdaWizard.App.Dialogs;

/// <summary>
/// og-styled modal dialog (DrwMessageBox family, 540-wide custom chrome)
/// with CUSTOM button labels — replaces the stock OK/Cancel MessageBox
/// (S-8: confirmLabel previously accepted and ignored).
/// </summary>
public partial class AppModalDialog : Window
{
    public static readonly DependencyProperty DialogTitleProperty =
        DependencyProperty.Register(nameof(DialogTitle), typeof(string), typeof(AppModalDialog), new(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(AppModalDialog), new(string.Empty));

    public static readonly DependencyProperty ConfirmLabelProperty =
        DependencyProperty.Register(nameof(ConfirmLabel), typeof(string), typeof(AppModalDialog), new("OK"));

    public static readonly DependencyProperty CancelLabelProperty =
        DependencyProperty.Register(nameof(CancelLabel), typeof(string), typeof(AppModalDialog), new("Cancel"));

    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ConfirmLabel
    {
        get => (string)GetValue(ConfirmLabelProperty);
        set => SetValue(ConfirmLabelProperty, value);
    }

    public string CancelLabel
    {
        get => (string)GetValue(CancelLabelProperty);
        set => SetValue(CancelLabelProperty, value);
    }

    /// <summary>True when the confirm button (or Enter) closed the dialog.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>Destructive confirms render the danger styling (og stop-scan:
    /// Stop in red). Set before Show.</summary>
    public bool Destructive { get; init; }

    public AppModalDialog()
    {
        InitializeComponent();
        Loaded += static (s, e) =>
        {
            var dialog = (AppModalDialog)s;
            if (dialog.Destructive)
            {
                dialog.ConfirmButton.SetResourceReference(System.Windows.Controls.Button.StyleProperty, "Button.Danger");
                dialog.SeverityGlyph.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "Brush.Status.DangerText");
            }
            // Focus the safest action: cancel unless destructive (og dialogs
            // focus Continue, not the destructive Stop).
            _ = (dialog.Destructive ? dialog.CancelButton : dialog.ConfirmButton).Focus();
        };
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Esc = cancel (IsCancel covers the button; also handle directly for
        // robustness with the custom chrome).
        if (e.Key == Key.Escape)
        {
            Confirmed = false;
            DialogResult = false;
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
