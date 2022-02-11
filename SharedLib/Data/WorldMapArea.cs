namespace SharedLib
{
    public readonly struct WorldMapArea
    {
        public int MapID { get; init; }
        public int AreaID { get; init; }
        public string AreaName { get; init; }
        public float LocLeft { get; init; }
        public float LocRight { get; init; }
        public float LocTop { get; init; }
        public float LocBottom { get; init; }
        public int UIMapId { get; init; }
        public string Continent { get; init; }


        public float ToWorldX(float value)
        {
            return ((LocBottom - LocTop) * value / 100) + LocTop;
        }

        public float ToWorldY(float value)
        {
            return ((LocRight - LocLeft) * value / 100) + LocLeft;
        }

        public float ToMapX(float value)
        {
            return 100 - (((value - LocBottom) * 100) / (LocTop - LocBottom));
        }

        public float ToMapY(float value)
        {
            return 100 - (((value - LocRight) * 100) / (LocLeft - LocRight));
        }

    }
}
