namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Components.Printer;
using LinuxDesktopApp.Helpers;
using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Views;

using LinuxDotNet.Cups;

using SkiaSharp;

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
            ms.Write("TEST1234567890\n"u8);
            ms.Seek(0, SeekOrigin.Begin);

            linePrinter.Print(ms);
        });
        PrintImageCommand = MakeDelegateCommand(() =>
        {
            using var bitmap = new SKBitmap(800, 600);
            using var canvas = new SKCanvas(bitmap);

            using var paint = new SKPaint();
            paint.Color = SKColors.Black;
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 2;
            paint.IsAntialias = true;

            // Background
            canvas.Clear(SKColors.White);

            // Rectangle
            canvas.DrawRect(50, 50, 700, 500, paint);

            // QR Code
            using var qrBitmap = SkiaHelper.CreateQrBitmap("TEST1234567890", 400 / 25);
            var qrX = (800 - qrBitmap.Width) / 2;
            var qrY = (600 - qrBitmap.Height) / 2;
            canvas.DrawBitmap(qrBitmap, qrX, qrY);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            // Print
            var options = new PrintOptions
            {
                Printer = printerSetting.ImagePrinterDevice,
                Copies = 1,
                MediaSize = "A4",
                ColorMode = true,
                Orientation = PrintOrientation.Portrait,
                Quality = PrintQuality.Normal
            };
            CupsPrinter.PrintStream(stream, options);
        });
    }
}
