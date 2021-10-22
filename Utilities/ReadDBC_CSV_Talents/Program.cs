namespace ReadDBC_CSV_Talents
{
    class Program
    {
        static void Main(string[] args)
        {
            var talentExtractor = new TalentExtractor();
            talentExtractor.Generate();
        }
    }
}
