namespace LinuxDesktopApp.Views.Example;

using Avalonia.Threading;

using LinuxDesktopApp.Components.Barcode;
using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Views;

using LinuxDotNet.InputEvent;

public sealed partial class BarcodeViewModel : AppViewModelBase
{
    private readonly IDispatcher dispatcher;

    private readonly BarcodeReader? barcodeReader;

    private readonly QrReader? qrReader;

    [ObservableProperty]
    public partial string Barcode { get; set; } = default!;

    public IObserveCommand ResumeCommand { get; }

    public IObserveCommand PauseCommand { get; }

    public BarcodeViewModel(
        IDispatcher dispatcher,
        BarcodeSetting barcodeSetting)
    {
        this.dispatcher = dispatcher;

        var device = EventDeviceInfo.GetDevices()
            .FirstOrDefault(x => x.Name.Contains(barcodeSetting.Name, StringComparison.OrdinalIgnoreCase))?.Device;
        if (!String.IsNullOrEmpty(device))
        {
            barcodeReader = new BarcodeReader(device);
            barcodeReader.BarcodeScanned += OnBarcodeScanned;
            barcodeReader.Start();
        }

        if (!String.IsNullOrEmpty(barcodeSetting.Port) && File.Exists(barcodeSetting.Port))
        {
            qrReader = new QrReader(barcodeSetting.Port);
            qrReader.QrScanned += OnBarcodeScanned;
            qrReader.Open();
        }

        ResumeCommand = MakeDelegateCommand(() =>
        {
            qrReader?.Resume();
        }, () => qrReader is not null);
        PauseCommand = MakeDelegateCommand(() =>
        {
            qrReader?.Pause();
        }, () => qrReader is not null);
    }

    private void OnBarcodeScanned(string code)
    {
        dispatcher.Post(() =>
        {
            Barcode = code;
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            barcodeReader?.Stop();
            barcodeReader?.Dispose();

            qrReader?.Resume();
            qrReader?.Close();
            qrReader?.Dispose();
        }

        base.Dispose(disposing);
    }
}
