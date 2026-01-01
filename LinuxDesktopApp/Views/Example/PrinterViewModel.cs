namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class PrinterViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public PrinterViewModel()
    {
        Message = "Hello from PrinterViewModel!";
        NavigateCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Menu);
        });
    }
}
