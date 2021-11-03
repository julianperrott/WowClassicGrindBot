using Core.Database;
using System.Drawing;

namespace Core
{
    public interface IAddonReader
    {
        bool Active { get; set; }

        PlayerReader PlayerReader { get; set; }

        BagReader BagReader { get; set; }
        EquipmentReader equipmentReader { get; set; }

        ActionBarCostReader ActionBarCostReader { get; set; }

        LevelTracker LevelTracker { get; set; }

        WorldMapAreaDB WorldMapAreaDb { get; set; }

        // player Reader props
        double AvgUpdateLatency { get; }

        int CombatCreatureCount { get; }

        string TargetName { get; }


        // functions

        void Refresh();
        void Reset();

        Color GetColorAt(int index);
        int GetIntAt(int index);
    }
}