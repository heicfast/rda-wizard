using System.Windows.Controls;
using System.Windows.Input;
using RdaWizard.Core.Models;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Landing page (og: PeripheralHomeView) — device selection + scan CTA.
/// </summary>
public partial class LandingView : UserControl
{
    public LandingView() => InitializeComponent();

    private void OnDiskCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: DiskInfo disk } && DataContext is LandingViewModel vm)
        {
            vm.SelectDiskCommand.Execute(disk);
        }
    }

    private void OnVolumeRowClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { Tag: VolumeInfo volume } && DataContext is LandingViewModel vm)
        {
            vm.SelectVolumeCommand.Execute(volume);
        }
    }
}
