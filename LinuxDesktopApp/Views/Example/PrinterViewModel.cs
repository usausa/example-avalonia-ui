namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class PrinterViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public PrinterViewModel()
    {
        Message = "Hello from PrinterViewModel!";
    }
}
