namespace LinuxDesktopApp.Components.Motor;

using System.Buffers;
using System.Buffers.Text;
using System.IO.Ports;

internal enum ServoChannel
{
    Servo1 = 1,
    Servo2 = 2
}

internal sealed class MotorController : IDisposable
{
    private readonly SerialPort port;

    public bool IsOpen => port.IsOpen;

    public MotorController(string name)
    {
        port = new SerialPort(name);
        port.BaudRate = 115200;
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

    public void SetLed(byte r, byte g, byte b)
    {
        if (!IsOpen)
        {
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(32);
        try
        {
            "LED "u8.CopyTo(buffer);
            var pos = 4;

            Utf8Formatter.TryFormat(r, buffer.AsSpan(pos), out var bytesWritten);
            pos += bytesWritten;
            buffer[pos++] = (byte)' ';

            Utf8Formatter.TryFormat(g, buffer.AsSpan(pos), out bytesWritten);
            pos += bytesWritten;
            buffer[pos++] = (byte)' ';

            Utf8Formatter.TryFormat(b, buffer.AsSpan(pos), out bytesWritten);
            pos += bytesWritten;

            buffer[pos++] = (byte)'\n';

            port.Write(buffer, 0, pos);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void SetServo(ServoChannel channel, int angle)
    {
        if (!IsOpen)
        {
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(32);
        try
        {
            "SERVO"u8.CopyTo(buffer);
            var pos = 5;

            buffer[pos++] = (byte)((int)channel + '0');
            buffer[pos++] = (byte)' ';

            Utf8Formatter.TryFormat(angle, buffer.AsSpan(pos), out var bytesWritten);
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
