using CommunityToolkit.Mvvm.ComponentModel;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// ViewModel base: INPC via the toolkit source generator + an
/// explicitly-marshaled <see cref="PostOnUi"/> helper. ViewModels never
/// touch Dispatcher types (Core has zero WPF references); the App layer
/// installs the marshaler at startup so engine-thread property updates
/// arrive on the UI thread without each call site caring.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private static Action<Action>? _uiMarshaler;

    /// <summary>Installed once by App startup; defaults to inline execution (unit tests).</summary>
    public static void InstallUiMarshaler(Action<Action> marshaler) => _uiMarshaler = marshaler;

    /// <summary>Runs <paramref name="action"/> on the UI thread when a marshaler is installed.</summary>
    protected static void PostOnUi(Action action)
    {
        var marshaler = _uiMarshaler;
        if (marshaler is null)
        {
            action();
        }
        else
        {
            marshaler(action);
        }
    }

    /// <summary>Raises <see cref="ObservableObject.PropertyChanged"/> for <paramref name="propertyName"/> on the UI thread.</summary>
    protected void RaiseOnUi(string propertyName) => PostOnUi(() => OnPropertyChanged(propertyName));
}
