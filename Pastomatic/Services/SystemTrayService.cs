using H.NotifyIcon;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Pastomatic.Services
{
    public class SystemTrayService : ISystemTrayService
    {
        private readonly ILogger<SystemTrayService> _logger;
        private readonly IIconManager _iconManager;
        private TaskbarIcon? _trayIcon;
        private bool _disposed;

        public event EventHandler? ShowSettingsRequested;
        public event EventHandler? ExitRequested;

        public SystemTrayService(ILogger<SystemTrayService> logger, IIconManager iconManager)
        {
            _logger = logger;
            _iconManager = iconManager;
        }

        public void Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SystemTrayService));

            _logger.LogInformation("Initializing system tray service");

            _iconManager.Initialize();
            CreateTrayIcon();

            _logger.LogInformation("System tray service initialized successfully");
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Pastomatic - Ready",
                IconSource = _iconManager.GetIcon(TrayIconStatus.Idle),
                Visibility = Visibility.Visible
            };

            var contextMenu = new ContextMenu();

            var statusItem = new MenuItem
            {
                Header = "Status: Ready",
                IsEnabled = false,
                Name = "StatusMenuItem"
            };
            contextMenu.Items.Add(statusItem);
            contextMenu.Items.Add(new Separator());

            var settingsItem = new MenuItem { Header = "Settings..." };
            settingsItem.Click += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenu = contextMenu;
            _trayIcon.ForceCreate();

            _logger.LogInformation("Tray icon created");
        }

        public void UpdateStatus(TrayIconStatus status)
        {
            if (_disposed) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_trayIcon == null) return;

                _trayIcon.IconSource = _iconManager.GetIcon(status);

                if (_trayIcon.ContextMenu != null)
                {
                    foreach (var item in _trayIcon.ContextMenu.Items)
                    {
                        if (item is MenuItem menuItem && menuItem.Name == "StatusMenuItem")
                        {
                            menuItem.Header = $"Status: {GetStatusText(status)}";
                            break;
                        }
                    }
                }
            });
        }

        private string GetStatusText(TrayIconStatus status)
        {
            return status switch
            {
                TrayIconStatus.Idle => "Ready",
                TrayIconStatus.Processing => "Processing...",
                TrayIconStatus.Success => "Success",
                TrayIconStatus.Error => "Error",
                _ => "Unknown"
            };
        }

        public void ShowBalloonTip(string title, string message)
        {
            if (_trayIcon == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _trayIcon.ShowNotification(title, message);
            });
        }

        public void SetToolTipText(string text)
        {
            if (_trayIcon == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _trayIcon.ToolTipText = text;
            });
        }

        public void Dispose()
        {
            if (_disposed) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_trayIcon != null)
                {
                    _trayIcon.IconSource = null;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            });

            _disposed = true;
            _logger.LogInformation("SystemTrayService disposed");
        }
    }
}
