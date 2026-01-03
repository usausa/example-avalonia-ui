namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class CameraViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public CameraViewModel()
    {
        Message = "Hello from CameraViewModel!";
    }
}
