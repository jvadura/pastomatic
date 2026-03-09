using System.Windows.Media;

namespace Pastomatic.Services
{
    public interface IIconManager
    {
        void Initialize();
        ImageSource GetIcon(TrayIconStatus status);
    }
}
