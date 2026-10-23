using System.Windows.Controls;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Landing page (og: PeripheralHomeView) — volume tiles + location trio +
/// scan CTA. All interaction flows through commands; no click code-behind.
/// </summary>
public partial class LandingView : UserControl
{
    public LandingView() => InitializeComponent();
}
