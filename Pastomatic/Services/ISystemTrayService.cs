using System;

namespace Pastomatic.Services
{
    public enum TrayIconStatus
    {
        Idle,
        Processing,
        Success,
        Error
    }

    public interface ISystemTrayService : IDisposable
    {
        event EventHandler ShowSettingsRequested;
        event EventHandler ExitRequested;

        void Initialize();
        void UpdateStatus(TrayIconStatus status);
        void ShowBalloonTip(string title, string message);
        void SetToolTipText(string text);
    }
}
