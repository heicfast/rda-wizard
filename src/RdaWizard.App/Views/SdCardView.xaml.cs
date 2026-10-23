using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>SD Card destination page (og PG-20).</summary>
public partial class SdCardView : UserControl
{
    public SdCardView() => InitializeComponent();

    private void OnShowAllDevicesClick(object sender, System.Windows.RoutedEventArgs e)
    {
        App.Services.GetRequiredService<MainViewModel>().TryOpenAuxiliary(PageKey.Landing);
    }
}
