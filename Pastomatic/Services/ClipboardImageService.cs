using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pastomatic.Services
{
    public class ClipboardImageService : IClipboardImageService
    {
        private readonly ILogger<ClipboardImageService> _logger;

        public ClipboardImageService(ILogger<ClipboardImageService> logger)
        {
            _logger = logger;
        }

        public bool HasImage()
        {
            try
            {
                return Clipboard.ContainsImage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check clipboard for image");
                return false;
            }
        }

        public BitmapSource? GetImage()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                    return null;

                var image = Clipboard.GetImage();
                if (image != null && !image.IsFrozen)
                    image.Freeze();

                _logger.LogInformation("Got clipboard image: {Width}x{Height}", image?.PixelWidth, image?.PixelHeight);
                return image;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get image from clipboard");
                return null;
            }
        }

        public BitmapSource ResizeImage(BitmapSource source, double maxMegapixels)
        {
            int currentPixels = source.PixelWidth * source.PixelHeight;
            int targetPixels = (int)(maxMegapixels * 1_000_000);

            if (currentPixels <= targetPixels)
            {
                _logger.LogDebug("Image already within size limit ({Current} <= {Target} pixels)", currentPixels, targetPixels);
                return source;
            }

            double scale = Math.Sqrt((double)targetPixels / currentPixels);
            int newWidth = (int)(source.PixelWidth * scale);
            int newHeight = (int)(source.PixelHeight * scale);

            _logger.LogInformation("Resizing image from {OldW}x{OldH} to {NewW}x{NewH}",
                source.PixelWidth, source.PixelHeight, newWidth, newHeight);

            var scaleTransform = new ScaleTransform(scale, scale);
            var resized = new TransformedBitmap(source, scaleTransform);

            // Convert to writable bitmap to ensure proper encoding
            var renderTarget = new RenderTargetBitmap(
                newWidth, newHeight, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawImage(resized, new Rect(0, 0, newWidth, newHeight));
            }
            renderTarget.Render(visual);
            renderTarget.Freeze();

            return renderTarget;
        }

        public byte[] EncodeAsPng(BitmapSource image)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        public void CopyTextToClipboard(string text)
        {
            try
            {
                Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, text), true);
                _logger.LogInformation("Copied {Length} chars to clipboard", text.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy text to clipboard");
                throw;
            }
        }
    }
}
