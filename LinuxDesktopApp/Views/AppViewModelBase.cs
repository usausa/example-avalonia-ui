namespace LinuxDesktopApp.Views;

using LinuxDesktopApp.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public abstract class AppViewModelBase : ExtendViewModelBase, INavigatorAware, INavigationEventSupport, INotifySupportAsync<ShellEvent>
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

    public async Task NavigatorNotifyAsync(ShellEvent parameter)
    {
        var task = parameter switch
        {
            ShellEvent.Start => OnNotifyStart(),
            _ => Task.CompletedTask
        };

        await task.ConfigureAwait(true);
    }

    protected virtual Task OnNotifyStart() => Task.CompletedTask;
}
