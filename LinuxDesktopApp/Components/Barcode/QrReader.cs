namespace LinuxDesktopApp.Components.Barcode;

using System.IO.Ports;

using Mofucat.SerialIO;

internal sealed class QrReader : IDisposable
{
    public event Action<string>? QrScanned;

    private static readonly byte[] ResumeCommand = "R\r"u8.ToArray();
    private static readonly byte[] PauseCommand = "Z\r"u8.ToArray();

    private readonly SerialPort port;

    private readonly SerialLineReader reader;

    public bool IsOpen => port.IsOpen;

    public QrReader(string name)
    {
        port = new SerialPort(name)
        {
            BaudRate = 19200,
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
            Handshake = Handshake.None,

            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        reader = new SerialLineReader(port, delimiter: [(byte)'\r']);
        reader.LineReceived += OnLineReceived;
    }

    private void OnLineReceived(object? sender, ReadOnlySpan<byte> bytes)
    {
        QrScanned?.Invoke(Encoding.UTF8.GetString(bytes));
    }

    public void Dispose()
    {
        reader.LineReceived -= OnLineReceived;
        reader.Dispose();
        port.Dispose();
    }

    public void Open()
    {
        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();
    }

    public void Close()
    {
        port.Close();
    }

    public void Resume()
    {
        port.Write(ResumeCommand, 0, ResumeCommand.Length);
    }

    public void Pause()
    {
        port.Write(PauseCommand, 0, PauseCommand.Length);
    }
}
