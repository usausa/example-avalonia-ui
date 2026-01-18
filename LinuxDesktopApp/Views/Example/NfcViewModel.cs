namespace LinuxDesktopApp.Views.Example;

using Avalonia.Threading;

using LinuxDesktopApp.Components.Nfc;
using LinuxDesktopApp.Views;

public sealed partial class NfcViewModel : AppViewModelBase
{
    private readonly IDispatcher dispatcher;

    private readonly PaSoRiSuicaReader reader;

    [ObservableProperty]
    public partial string Id { get; set; } = default!;

    public NfcViewModel(
        IDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;

        reader = new PaSoRiSuicaReader();
        reader.SuicaRead += ReaderOnCardRead;

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

    private void ReaderOnCardRead(object? sender, SuicaReadEventArgs e)
    {
        dispatcher.Post(() => Id = Convert.ToHexString(e.IDm));
        // TODO
    }
}
