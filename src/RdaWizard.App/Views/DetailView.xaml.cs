using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Detail page (og: CDetailPage): scan progress + results on one surface.
/// </summary>
public partial class DetailView : UserControl
{
    public DetailView() => InitializeComponent();

    private static MainViewModel Main => App.Services.GetRequiredService<MainViewModel>();

    private void OnHomeClick(object sender, System.Windows.RoutedEventArgs e)
    {
        // og toolbar Home: returns to the landing (a running scan continues
        // app-level in the background — the og runs CScanMgr outside the page).
        Main.TryOpenAuxiliary(PageKey.Landing);
    }
}
