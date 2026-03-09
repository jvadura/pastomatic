using System.Windows.Media.Imaging;

namespace Pastomatic.Services
{
    public interface IClipboardImageService
    {
        bool HasImage();
        BitmapSource? GetImage();
        BitmapSource ResizeImage(BitmapSource source, double maxMegapixels);
        byte[] EncodeAsPng(BitmapSource image);
        void CopyTextToClipboard(string text);
    }
}
