using System.Windows;

namespace RdaWizard.App.Dialogs;

/// <summary>
/// Transitional recovery progress dialog (og: RecoverProgressDialog family).
/// Binds to the RecoveryViewModel; the shell owns show/close.
/// </summary>
public partial class RecoveryProgressDialog : Window
{
    public RecoveryProgressDialog()
    {
        InitializeComponent();
    }
}
