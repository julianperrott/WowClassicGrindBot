using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;

namespace PathMaker
{
    class Program
    {
        public static string DataPath = "../../../../data/";

        static void Main(string[] args)
        {
            string inputFile = "input.txt";
            string outputFile = "output.json";

            List<Vector2> coordinates = new();

            foreach (string line in File.ReadLines(Path.Join(DataPath, inputFile)))
            {
                var split = line.Split(' ');
                coordinates.Add(new Vector2(float.Parse(split[0]), float.Parse(split[1])));
            }

            var sorted = SortByNextClosesDistance(coordinates);

            var text = JsonConvert.SerializeObject(sorted);
            File.WriteAllText(Path.Join(DataPath, outputFile), text);
        }

        private static Vector2[] SortByNextClosesDistance(List<Vector2> nodes)
        {
            Stack<Vector2> output = new();

            output.Push(nodes[0]);
            nodes.RemoveAt(0);

            while (nodes.Count != 0)
            {
                int closestIndex = 0;
                float closestDistance = float.MaxValue;

                for (int i = 0; i < nodes.Count; i++)
                {
                    float d = Vector2.DistanceSquared(output.Peek(), nodes[i]);
                    if (d < closestDistance)
                    {
                        closestIndex = i;
                        closestDistance = d;
                    }
                }

                if (closestIndex != -1)
                {
                    output.Push(nodes[closestIndex]);
                    nodes.RemoveAt(closestIndex);
                }
            }

            return output.ToArray();
        }

    }
}
