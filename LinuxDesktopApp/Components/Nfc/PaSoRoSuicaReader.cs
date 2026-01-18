namespace LinuxDesktopApp.Components.Nfc;

using LinuxDesktopApp.Domain.Logic;

using PCSC;
using PCSC.Monitoring;

public sealed class SuicaHistoryRecord
{
    public DateTime DateTime { get; }

    public byte Terminal { get; }

    public byte Process { get; }

    public int Balance { get; }

    public SuicaHistoryRecord(DateTime dateTime, byte terminal, byte process, int balance)
    {
        DateTime = dateTime;
        Terminal = terminal;
        Process = process;
        Balance = balance;
    }
}

#pragma warning disable CA1819
public sealed class SuicaReadEventArgs : EventArgs
{
    public byte[] IDm { get; }

    public int Balance { get; }

    public ReadOnlyCollection<SuicaHistoryRecord> History { get; }

    public SuicaReadEventArgs(byte[] idm, int balance, ReadOnlyCollection<SuicaHistoryRecord> history)
    {
        IDm = idm;
        Balance = balance;
        History = history;
    }
}
#pragma warning restore CA1819

internal sealed class PaSoRiSuicaReader : IDisposable
{
    public event EventHandler<SuicaReadEventArgs>? SuicaRead;

    private readonly ISCardMonitor monitor;

    public bool IsRunning { get; private set; }

    public PaSoRiSuicaReader()
    {
        monitor = MonitorFactory.Instance.Create(SCardScope.System);

        monitor.CardInserted += OnCardInserted;
    }

    public void Dispose()
    {
        MonitorFactory.Instance.Release(monitor);
        monitor.Dispose();
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

    private void OnCardInserted(object sender, CardStatusEventArgs e)
    {
        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var reader = context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
#pragma warning disable CA1031
        try
        {
            var response = SendCommand(reader, CreateCommand(0xFF, 0xCA, 0x00, 0x00, 0x00));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"1 {response.SW1:X2} {response.SW2:X2}");
                return;
            }

            var idm = response.Data.ToArray();

            // SELECT 0x008B
            response = SendCommand(reader, CreateCommand(0xFF, 0xA4, 0x00, 0x01, [0x8B, 0x00]));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"2 {response.SW1:X2} {response.SW2:X2}");
                return;
            }

            response = SendCommand(reader, CreateCommand(0xFF, 0xB0, 0x00, 0x00, 0x00));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"3 {response.SW1:X2} {response.SW2:X2}");
                return;
            }

            var balance = SuicaLogic.ExtractAccessBalance(response.Data);

            // SELECT 0x090F
            response = SendCommand(reader, CreateCommand(0xFF, 0xA4, 0x00, 0x01, [0x0F, 0x09]));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"4 {response.SW1:X2} {response.SW2:X2}");
                return;
            }

            Console.WriteLine($"5 {response.SW1:X2} {response.SW2:X2}");

            var records = new List<SuicaHistoryRecord>();
            for (var i = 0; i < 20; i++)
            {
                response = SendCommand(reader, CreateCommand(0xFF, 0xB0, 0x00, (byte)i, 0x00));
                if (!response.IsSuccess())
                {
                    return;
                }

                records.Add(new SuicaHistoryRecord(
                    SuicaLogic.ExtractLogDateTime(response.Data),
                    SuicaLogic.ExtractLogTerminal(response.Data),
                    SuicaLogic.ExtractLogProcess(response.Data),
                    SuicaLogic.ExtractLogBalance(response.Data)));
            }

            SuicaRead?.Invoke(this, new SuicaReadEventArgs(idm, balance, records.AsReadOnly()));
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

    private static Response SendCommand(ICardReader reader, byte[] command)
    {
        var receiveBuffer = new byte[258]; // SW1+SW2 included
        var bytesReceived = reader.Transmit(command, receiveBuffer);
        return new Response(receiveBuffer, bytesReceived);
    }

    private static byte[] CreateCommand(byte cla, byte ins, byte p1, byte p2, byte[] data)
    {
        var command = new byte[4 + 1 + data.Length];
        command[0] = cla;
        command[1] = ins;
        command[2] = p1;
        command[3] = p2;
        command[4] = (byte)data.Length; // Lc
        data.CopyTo(command.AsSpan(5, data.Length));
        return command;
    }

    private static byte[] CreateCommand(byte cla, byte ins, byte p1, byte p2, int le)
    {
        var command = new byte[5];
        command[0] = cla;
        command[1] = ins;
        command[2] = p1;
        command[3] = p2;
        command[4] = (byte)le; // Le
        return command;
    }

    private sealed class Response
    {
        private readonly byte[] buffer;

        private readonly int length;

        public ReadOnlySpan<byte> Data => buffer.AsSpan(0, length >= 2 ? length - 2 : 0);

        public byte SW1 { get; }

        public byte SW2 { get; }

        public Response(byte[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;

            if (length >= 2)
            {
                SW1 = buffer[length - 2];
                SW2 = buffer[length - 1];
            }
            else
            {
                SW1 = 0x00;
                SW2 = 0x00;
            }
        }

        public bool IsSuccess()
        {
            return SW1 == 0x90 && SW2 == 0x00;
        }
    }
}
