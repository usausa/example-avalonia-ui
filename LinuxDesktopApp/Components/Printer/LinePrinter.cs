namespace LinuxDesktopApp.Components.Printer;

public sealed class LinePrinter
{
    private readonly string device;

    public LinePrinter(string device)
    {
        this.device = device;
    }

    public void Print(MemoryStream stream)
    {
        using var fs = new FileStream(device, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.CopyTo(fs);
        fs.Flush(true);
    }
}
