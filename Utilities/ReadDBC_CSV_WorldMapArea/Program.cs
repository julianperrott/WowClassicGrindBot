using System;

namespace ReadDBC_CSV_WorldMapArea
{
    class Program
    {
        static void Main(string[] args)
        {
            var transfrom = new Transform();
            transfrom.CreateWorldMapAreaJson();

            transfrom.Validate();

            Console.WriteLine("\nExpected result:\nUnsupported mini maps areas: Expansion01, Sunwell");
        }
    }
}
