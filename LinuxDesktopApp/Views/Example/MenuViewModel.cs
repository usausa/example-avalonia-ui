namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class MenuViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public ICommand NavigateToDashboardCommand { get; }

    public ICommand NavigateToTypographyCommand { get; }

    public ICommand NavigateToBarcodeCommand { get; }

    public ICommand NavigateToCameraCommand { get; }

    public ICommand NavigateToPrinterCommand { get; }

    public MenuViewModel()
    {
        Message = "Hello from MenuViewModel!";

        NavigateToDashboardCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Dashboard);
        });

        NavigateToTypographyCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Typography);
        });

        NavigateToBarcodeCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Barcode);
        });

        NavigateToCameraCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Camera);
        });

        NavigateToPrinterCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Printer);
        });
    }
}
