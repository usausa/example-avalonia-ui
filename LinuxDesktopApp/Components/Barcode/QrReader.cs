namespace LinuxDesktopApp.Components.Barcode;

using System.IO.Ports;

internal sealed class QrReader : IDisposable
{
    public event Action<string>? QrScanned;

    private static readonly byte[] ResumeCommand = "R\r"u8.ToArray();
    private static readonly byte[] PauseCommand = "Z\r"u8.ToArray();

    private readonly SerialPort port;

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

        port.DataReceived += (sender, _) =>
        {
            var sp = (SerialPort)sender;
            var line = sp.ReadExisting();
            QrScanned?.Invoke(line);
        };
    }

    public void Dispose()
    {
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
