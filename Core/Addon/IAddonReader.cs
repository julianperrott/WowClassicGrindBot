using Core.Database;

namespace Core
{
    public interface IAddonReader
    {
        bool Active { get; set; }

        PlayerReader PlayerReader { get; }

        BagReader BagReader { get; }

        EquipmentReader EquipmentReader { get; }

        ActionBarCostReader ActionBarCostReader { get; }

        LevelTracker LevelTracker { get; }

        WorldMapAreaDB WorldMapAreaDb { get; }

        double AvgUpdateLatency { get; }

        int CombatCreatureCount { get; }

        string TargetName { get; }

        RecordInt UIMapId { get; }

        void Refresh();
        void Reset();

        int GetIntAt(int index);
    }
}