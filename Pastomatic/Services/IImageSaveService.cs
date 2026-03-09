namespace Pastomatic.Services
{
    public interface IImageSaveService
    {
        string SaveImage(byte[] pngBytes);
        string ToWslPath(string windowsPath);
        string FormatClipboardText(string wslPath);
    }
}
