using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pastomatic.Services;
using Pastomatic.Views;
using System;
using System.Windows;

namespace Pastomatic.ViewModels
{
    public class MainViewModel
    {
        private readonly IHotkeyService _hotkeyService;
        private readonly IClipboardImageService _clipboardService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MainViewModel> _logger;
        private ISystemTrayService? _trayService;
        private IServiceProvider? _services;

        public MainViewModel(
            IHotkeyService hotkeyService,
            IClipboardImageService clipboardService,
            IConfiguration configuration,
            ILogger<MainViewModel> logger)
        {
            _hotkeyService = hotkeyService;
            _clipboardService = clipboardService;
            _configuration = configuration;
            _logger = logger;

            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        public void Initialize(ISystemTrayService trayService, IServiceProvider services)
        {
            _trayService = trayService;
            _services = services;

            _trayService.ShowSettingsRequested += (s, e) => ShowSettings();
            _trayService.ExitRequested += (s, e) =>
            {
                _logger.LogInformation("Exit requested from tray");
                Application.Current.Shutdown();
            };
        }

        private void OnHotkeyPressed(object? sender, HotkeyAction action)
        {
            if (action == HotkeyAction.ShowPopup)
            {
                ShowPopup();
            }
        }

        private void ShowPopup()
        {
            _logger.LogInformation("Hotkey pressed - checking clipboard");

            if (!_clipboardService.HasImage())
            {
                _logger.LogInformation("No image in clipboard");
                _trayService?.ShowBalloonTip("Pastomatic", "No image in clipboard");
                return;
            }

            var vm = _services!.GetRequiredService<ActionWindowViewModel>();
            var maxMp = _configuration.GetValue<double>("Image:MaxMegapixels", 2.0);
            vm.LoadClipboardImage(maxMp);

            if (vm.PreviewImage == null)
            {
                _trayService?.ShowBalloonTip("Pastomatic", "Failed to read clipboard image");
                return;
            }

            var successDisplayMs = _configuration.GetValue<int>("UI:SuccessDisplayMs", 1500);
            var closeOnFocusLoss = _configuration.GetValue<bool>("UI:CloseOnFocusLoss", true);

            var window = new ActionWindow(vm, successDisplayMs, closeOnFocusLoss);
            window.Show();
        }

        private void ShowSettings()
        {
            _logger.LogInformation("Opening settings window");
            var settingsWindow = new Views.SettingsWindow(_configuration);
            settingsWindow.ShowDialog();
        }
    }
}
