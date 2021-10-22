namespace ReadDBC_CSV_Spell
{
    public class Spell
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int Level { get; set; }

        public override string ToString()
        {
            return $"{Id} - {Name} - {Level}";
        }
    }
}
