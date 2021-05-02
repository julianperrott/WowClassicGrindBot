using System.Drawing;

namespace Core
{
    public interface IAddonReader
    {
        PlayerReader PlayerReader { get; set; }

        void Refresh();

        Color GetColorAt(int index);

        BagReader BagReader { get; set; }
        EquipmentReader equipmentReader { get; set; }
        bool Active { get; set; }
        LevelTracker LevelTracker { get; set; }
    }
}