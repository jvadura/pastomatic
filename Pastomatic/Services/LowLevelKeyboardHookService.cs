using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace Pastomatic.Services
{
    public class LowLevelKeyboardHookService : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_SNAPSHOT = 0x2C;
        private const int VK_PAUSE = 0x13;
        private const int VK_SCROLL = 0x91;
        private const int VK_INSERT = 0x2D;
        private const int VK_ESCAPE = 0x1B;

        // CRITICAL: Store delegate as field to prevent garbage collection
        private LowLevelKeyboardProc? _hookProc;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly ILogger<LowLevelKeyboardHookService> _logger;

        private readonly Dictionary<int, HotkeyAction> _keyMappings = new();
        private readonly HashSet<int> _nonSuppressedKeys = new();

        private int _lastKeyPressed = 0;
        private DateTime _lastKeyPressTime = DateTime.MinValue;
        private const int DebounceMilliseconds = 100;
        private bool _isHookInstalled = false;

        public event EventHandler<HotkeyAction>? HotkeyPressed;

        public LowLevelKeyboardHookService(ILogger<LowLevelKeyboardHookService> logger)
        {
            _logger = logger;
            _hookProc = HookCallback;
        }

        public void RegisterHook(int vkCode, HotkeyAction action, bool suppressKey = true)
        {
            _keyMappings[vkCode] = action;

            if (!suppressKey)
                _nonSuppressedKeys.Add(vkCode);
            else
                _nonSuppressedKeys.Remove(vkCode);

            _logger.LogDebug("Registered VK 0x{VkCode:X2} for action {Action}", vkCode, action);
        }

        public void UnregisterHook(int vkCode)
        {
            _keyMappings.Remove(vkCode);
            _nonSuppressedKeys.Remove(vkCode);
        }

        public void ClearHooks()
        {
            _keyMappings.Clear();
            _nonSuppressedKeys.Clear();
        }

        public void InstallHook()
        {
            if (_isHookInstalled) return;

            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc!, IntPtr.Zero, 0);

            if (_hookID == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "Failed to install low-level keyboard hook");
            }

            _isHookInstalled = true;
            _logger.LogInformation("Low-level keyboard hook installed");
        }

        public void UninstallHook()
        {
            if (!_isHookInstalled || _hookID == IntPtr.Zero) return;

            if (UnhookWindowsHookEx(_hookID))
            {
                _hookID = IntPtr.Zero;
                _isHookInstalled = false;
                _logger.LogInformation("Low-level keyboard hook uninstalled");
            }
        }

        public bool IsInstalled => _isHookInstalled;

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    if (_keyMappings.TryGetValue(vkCode, out HotkeyAction action))
                    {
                        var now = DateTime.UtcNow;
                        if (vkCode == _lastKeyPressed &&
                            (now - _lastKeyPressTime).TotalMilliseconds < DebounceMilliseconds)
                        {
                            if (!_nonSuppressedKeys.Contains(vkCode))
                                return (IntPtr)1;
                            return CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }

                        _lastKeyPressed = vkCode;
                        _lastKeyPressTime = now;

                        Application.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                HotkeyPressed?.Invoke(this, action);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error invoking HotkeyPressed for {Action}", action);
                            }
                        });

                        if (!_nonSuppressedKeys.Contains(vkCode))
                            return (IntPtr)1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in keyboard hook callback");
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static int GetVirtualKeyCodeFromName(string? keyName)
        {
            return keyName?.ToLowerInvariant() switch
            {
                "printscreen" => VK_SNAPSHOT,
                "pause" => VK_PAUSE,
                "pause/break" => VK_PAUSE,
                "scrolllock" => VK_SCROLL,
                "scroll lock" => VK_SCROLL,
                "insert" => VK_INSERT,
                "escape" => VK_ESCAPE,
                "esc" => VK_ESCAPE,
                _ => 0
            };
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public void Dispose()
        {
            UninstallHook();
            _hookProc = null;
        }
    }

    public enum HotkeyAction
    {
        ShowPopup
    }
}
