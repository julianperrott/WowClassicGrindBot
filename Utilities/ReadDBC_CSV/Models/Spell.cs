namespace ReadDBC_CSV
{
    public struct Spell
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }

        public void SetLevel(int level)
        {
            Level = level;
        }
    }
}
