using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Pastomatic.Services
{
    public class HotkeyService : IHotkeyService
    {
        private readonly ILogger<HotkeyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly LowLevelKeyboardHookService _hookService;
        private bool _disposed;

        public event EventHandler<HotkeyAction>? HotkeyPressed;

        public HotkeyService(
            ILogger<HotkeyService> logger,
            IConfiguration configuration,
            LowLevelKeyboardHookService hookService)
        {
            _logger = logger;
            _configuration = configuration;
            _hookService = hookService;

            _hookService.HotkeyPressed += (sender, action) => HotkeyPressed?.Invoke(this, action);
        }

        public void RegisterHotkeys()
        {
            var keyName = _configuration.GetValue<string>("Hotkey:Key", "Insert")!;
            var suppress = _configuration.GetValue<bool>("Hotkey:Suppress", true);

            var vkCode = LowLevelKeyboardHookService.GetVirtualKeyCodeFromName(keyName);
            if (vkCode == 0)
            {
                _logger.LogError("Invalid hotkey key name: {KeyName}", keyName);
                return;
            }

            _hookService.RegisterHook(vkCode, HotkeyAction.ShowPopup, suppress);
            _hookService.InstallHook();

            _logger.LogInformation("Hotkey registered: {Key} (VK 0x{VkCode:X2}, suppress={Suppress})",
                keyName, vkCode, suppress);
        }

        public void UnregisterHotkeys()
        {
            _hookService.ClearHooks();
            _hookService.UninstallHook();
            _logger.LogInformation("Hotkeys unregistered");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterHotkeys();
                _hookService?.Dispose();
                _disposed = true;
            }
        }
    }
}
