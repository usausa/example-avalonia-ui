namespace LinuxDesktopApp;

using LinuxDesktopApp.Views;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainWindowViewModel : ExtendViewModelBase
{
    public Navigator Navigator { get; set; }

    public IObserveCommand ForwardCommand { get; }

    public MainWindowViewModel(Navigator navigator)
    {
        Navigator = navigator;

        ForwardCommand = MakeDelegateCommand<ViewId>(x => Navigator.Forward(x));
    }
}
