namespace LinuxDesktopApp.Views.Example;

using Avalonia.Threading;

using LinuxDesktopApp.Components.Nfc;
using LinuxDesktopApp.Views;

public sealed partial class NfcViewModel : AppViewModelBase
{
    private readonly IDispatcher dispatcher;

    private readonly PaSoRiS300Reader reader;

    [ObservableProperty]
    public partial string Id { get; set; } = default!;

    public NfcViewModel(
        IDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;

        reader = new PaSoRiS300Reader();
        reader.CardDetected += ReaderOnCardDetected;

        reader.Start();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            reader.Stop();
            reader.Dispose();
        }
    }

    private void ReaderOnCardDetected(object? sender, CardDetectEventArgs e)
    {
        dispatcher.Post(() => Id = Convert.ToHexString(e.IDm));
    }
}
