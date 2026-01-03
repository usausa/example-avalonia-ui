namespace LinuxDesktopApp.Views.Example;

using LinuxDotNet.Cups;

using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Views;

public sealed class PrinterViewModel : AppViewModelBase
{
    public IObserveCommand PrintTextCommand { get; }

    public IObserveCommand PrintImageCommand { get; }

    public PrinterViewModel(PrinterSetting printerSetting)
    {
        PrintTextCommand = MakeDelegateCommand(() =>
        {
            using var ms = new MemoryStream();
            ms.Write("TEST1234567890\n"u8);
            ms.Seek(0, SeekOrigin.Begin);

            CupsPrinter.PrintStream(ms, new PrintOptions
            {
                Printer = printerSetting.TextPrinter,
                MediaType = "text/plain"
            });
        });
        PrintImageCommand = MakeDelegateCommand(() =>
        {
            // TODO
        });
    }
}
