namespace PosApp.Views;

using Smart.Avalonia.ViewModels;

public abstract class AppViewModelBase : ViewModelBase, INavigatorAware, INavigationEventSupport
{
    public INavigator Navigator { get; set; } = default!;

    public void OnNavigatingFrom(INavigationContext context)
    {
    }

    public void OnNavigatingTo(INavigationContext context)
    {
    }

    public void OnNavigatedTo(INavigationContext context)
    {
    }
}
