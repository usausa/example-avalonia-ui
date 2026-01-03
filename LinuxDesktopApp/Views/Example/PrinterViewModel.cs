namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Components.Printer;
using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Views;

public sealed class PrinterViewModel : AppViewModelBase
{
    public IObserveCommand PrintTextCommand { get; }

    public IObserveCommand PrintImageCommand { get; }

    public PrinterViewModel(PrinterSetting printerSetting)
    {
        var linePrinter = new LinePrinter(printerSetting.LinePrinterDevice);

        PrintTextCommand = MakeDelegateCommand(() =>
        {
            using var ms = new MemoryStream();
            ms.Write("TEST-1234567890\n"u8);
            ms.Seek(0, SeekOrigin.Begin);

            linePrinter.Print(ms);
        });
        PrintImageCommand = MakeDelegateCommand(() =>
        {
            // TODO
        });
    }
}
