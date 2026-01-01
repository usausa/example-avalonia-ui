namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class DashboardViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public DashboardViewModel()
    {
        Message = "Hello from DashboardViewModel!";
        NavigateCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Menu);
        });
    }
}
