using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pastomatic.Services
{
    public class IconManager : IIconManager
    {
        private readonly Dictionary<TrayIconStatus, ImageSource> _iconCache = new();
        private readonly ILogger<IconManager> _logger;

        public IconManager(ILogger<IconManager> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing icon cache");

            _iconCache[TrayIconStatus.Idle] = LoadBitmapImage("idle.ico");
            _iconCache[TrayIconStatus.Processing] = LoadBitmapImage("processing.ico");
            _iconCache[TrayIconStatus.Success] = LoadBitmapImage("success.ico");
            _iconCache[TrayIconStatus.Error] = LoadBitmapImage("error.ico");

            foreach (var bitmap in _iconCache.Values.OfType<BitmapImage>())
            {
                if (!bitmap.IsFrozen)
                    bitmap.Freeze();
            }

            _logger.LogInformation("Icon cache initialized with {Count} icons", _iconCache.Count);
        }

        public ImageSource GetIcon(TrayIconStatus status)
        {
            if (_iconCache.TryGetValue(status, out var icon))
                return icon;

            _logger.LogWarning("Icon not found for status: {Status}, returning Idle icon", status);
            return _iconCache[TrayIconStatus.Idle];
        }

        private BitmapImage LoadBitmapImage(string filename)
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", filename);

            if (!File.Exists(iconPath))
            {
                _logger.LogError("Icon file not found: {Path}", iconPath);
                throw new FileNotFoundException($"Icon file not found: {iconPath}");
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            return bitmap;
        }
    }
}
