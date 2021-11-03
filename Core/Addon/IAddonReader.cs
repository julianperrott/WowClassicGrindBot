using Core.Database;

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

        double AvgUpdateLatency { get; }

        int CombatCreatureCount { get; }

        string TargetName { get; }

        RecordInt UIMapId { get; }

        void Refresh();
        void Reset();

        int GetIntAt(int index);
    }
}