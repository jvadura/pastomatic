using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Pastomatic.Services
{
    public class ImageSaveService : IImageSaveService
    {
        private readonly ILogger<ImageSaveService> _logger;
        private readonly IConfiguration _configuration;

        public ImageSaveService(ILogger<ImageSaveService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public string SaveImage(byte[] pngBytes)
        {
            var saveFolder = _configuration.GetValue<string>("Image:SaveFolder", @"E:\claude\images")!;

            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);

            var filename = $"{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var fullPath = Path.Combine(saveFolder, filename);

            File.WriteAllBytes(fullPath, pngBytes);
            _logger.LogInformation("Saved image to {Path} ({Size} bytes)", fullPath, pngBytes.Length);

            return fullPath;
        }

        public string ToWslPath(string windowsPath)
        {
            // E:\claude\images\file.png → /mnt/e/claude/images/file.png
            var drive = char.ToLower(windowsPath[0]);
            var rest = windowsPath.Substring(3).Replace('\\', '/');
            return $"/mnt/{drive}/{rest}";
        }

        public string FormatClipboardText(string wslPath)
        {
            var format = _configuration.GetValue<string>("Image:ClipboardFormat", "Read {wslpath}")!;
            return format.Replace("{wslpath}", wslPath);
        }
    }
}
