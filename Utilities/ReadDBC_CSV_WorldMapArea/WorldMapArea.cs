namespace ReadDBC_CSV_WorldMapArea
{
    public class WorldMapArea
    {
        public int ID { get; set; }
        public int MapID { get; set; }
        public int AreaID { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public float LocLeft { get; set; }
        public float LocRight { get; set; }
        public float LocTop { get; set; }
        public float LocBottom { get; set; }
        public int UIMapId { get; set; }
        public string Continent { get; set; } = string.Empty;
    }
}
