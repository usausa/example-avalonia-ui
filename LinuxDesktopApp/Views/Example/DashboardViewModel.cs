namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class DashboardViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public DashboardViewModel()
    {
        Message = "Hello from DashboardViewModel!";
    }
}
