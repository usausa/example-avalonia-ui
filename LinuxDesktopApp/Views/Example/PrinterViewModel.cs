namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class PrinterViewModel : AppViewModelBase
{
    public IObserveCommand PrintTextCommand { get; }

    public IObserveCommand PrintImageCommand { get; }

    public PrinterViewModel()
    {
        PrintTextCommand = MakeDelegateCommand(() =>
        {
            // TODO
        });
        PrintImageCommand = MakeDelegateCommand(() =>
        {
            // TODO
        });
    }
}
