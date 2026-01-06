namespace LinuxDesktopApp.Helpers;

using QRCoder;

using SkiaSharp;

internal static class SkiaHelper
{
    public static SKBitmap CreateQrBitmap(string text, int pixelPerModule)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelPerModule);
        using var ms = new MemoryStream(qrCodeBytes);
        return SKBitmap.Decode(ms);
    }
}
