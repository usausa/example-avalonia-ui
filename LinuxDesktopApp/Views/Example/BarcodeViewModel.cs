namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class BarcodeViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public BarcodeViewModel()
    {
        Message = "Hello from BarcodeViewModel!";
    }
}
