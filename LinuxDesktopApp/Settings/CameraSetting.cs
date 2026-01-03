namespace LinuxDesktopApp.Settings;

public sealed class CameraSetting
{
    public string Device { get; set; } = default!;

    public int Width { get; set; }

    public int Height { get; set; }

    public int Fps { get; set; }
}
