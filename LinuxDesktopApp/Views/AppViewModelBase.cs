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
            ShellEvent.Trigger1 => OnNotifyTrigger1(),
            ShellEvent.Trigger2 => OnNotifyTrigger2(),
            ShellEvent.Trigger3 => OnNotifyTrigger3(),
            ShellEvent.Trigger4 => OnNotifyTrigger4(),
            _ => Task.CompletedTask
        };

        await task.ConfigureAwait(true);
    }

    protected virtual Task OnNotifyTrigger1() => Task.CompletedTask;

    protected virtual Task OnNotifyTrigger2() => Task.CompletedTask;

    protected virtual Task OnNotifyTrigger3() => Task.CompletedTask;

    protected virtual Task OnNotifyTrigger4() => Task.CompletedTask;
}
