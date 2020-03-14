using System.Drawing;

namespace Libs
{
    public interface IAddonReader
    {
        PlayerReader? PlayerReader { get; set; }
        void Refresh();
        Color GetColorAt(int index);
    }
}