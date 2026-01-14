namespace LinuxDesktopApp.Components.Motor;

using System.Buffers.Text;
using System.IO.Ports;

internal enum MotorChannel
{
    Motor1,
    Motor2
}

// TODO delete
#pragma warning disable CA1812
internal sealed class MotorController : IDisposable
{
    private readonly SerialPort port;

    public bool IsOpen => port.IsOpen;

    public MotorController(string name)
    {
        port = new SerialPort(name);
        port.BaudRate = 1150200;
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

    public void SetSpeed(MotorChannel channel, int value)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(32);
        try
        {
            "+W "u8.CopyTo(buffer);
            var pos = 3;

            buffer[pos++] = channel == MotorChannel.Motor2 ? (byte)'2' : (byte)'1';
            buffer[pos++] = (byte)',';

            Utf8Formatter.TryFormat(value, buffer.AsSpan(pos), out var bytesWritten);
            pos += bytesWritten;

            buffer[pos++] = (byte)'\n';

            port.Write(buffer, 0, pos);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
