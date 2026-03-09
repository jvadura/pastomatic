using System;

namespace Pastomatic.Services
{
    public interface IHotkeyService : IDisposable
    {
        event EventHandler<HotkeyAction> HotkeyPressed;
        void RegisterHotkeys();
        void UnregisterHotkeys();
    }
}
