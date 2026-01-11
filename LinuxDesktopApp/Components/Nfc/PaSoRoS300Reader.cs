namespace LinuxDesktopApp.Components.Nfc;

using PCSC;
using PCSC.Monitoring;

#pragma warning disable CA1819
public sealed class CardDetectEventArgs : EventArgs
{
    public byte[] IDm { get; }

    public byte[] PMm { get; }

    public CardDetectEventArgs(byte[] idm, byte[] pmm)
    {
        IDm = idm;
        PMm = pmm;
    }
}
#pragma warning restore CA1819

internal sealed class PaSoRiS300Reader : IDisposable
{
    public event EventHandler<CardDetectEventArgs>? CardDetected;

    private readonly ISCardMonitor monitor;

    public bool IsRunning { get; private set; }

    public PaSoRiS300Reader()
    {
        monitor = MonitorFactory.Instance.Create(SCardScope.System);

        monitor.CardInserted += OnCardInserted;
    }

    public void Dispose()
    {
        MonitorFactory.Instance.Release(monitor);
        monitor.Dispose();
    }

    private void OnCardInserted(object sender, CardStatusEventArgs e)
    {
        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var reader = context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
#pragma warning disable CA1031
        try
        {
            var pollingCommand = new byte[5];
            pollingCommand[0] = 0x00; // Polling
            pollingCommand[1] = 0xFF; // SystemCode
            pollingCommand[2] = 0xFF;
            pollingCommand[3] = 0x01; // Request Code
            pollingCommand[4] = 0x00; // Time Slot

            var request = new byte[pollingCommand.Length + 5];
            request[0] = 0xFF; // CLA
            request[1] = 0xFE; // INS
            request[2] = 0x00; // P1
            request[3] = 0x00; // P2
            request[4] = (byte)pollingCommand.Length; // Lc
            pollingCommand.AsSpan().CopyTo(request.AsSpan(5, pollingCommand.Length));

            var response = new byte[256];
            var length = reader.Transmit(request, response);

            if ((length < 2) ||
                (response[length - 2] != 0x90) ||
                (response[length - 1] != 0x00))
            {
                return;
            }

            var idm = new byte[8];
            response.AsSpan(1, 8).CopyTo(idm);
            var pmm = new byte[8];
            response.AsSpan(9, 8).CopyTo(pmm);

            CardDetected?.Invoke(this, new CardDetectEventArgs(idm, pmm));
        }
        catch
        {
            // Ignore
        }
        finally
        {
            reader.Disconnect(SCardReaderDisposition.Leave);
        }
#pragma warning restore CA1031
    }

    public bool Start()
    {
        if (IsRunning)
        {
            return false;
        }

        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var readers = context.GetReaders();
        if (readers.Length == 0)
        {
            return false;
        }

        monitor.Start(readers[0]);

        IsRunning = true;

        return true;
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        monitor.Cancel();

        IsRunning = false;
    }
}
