using System;
using System.Collections.Generic;
using System.IO;

namespace ReadDBC_CSV
{
    public class CSVExtractor
    {
        public readonly List<string> ColumnIndexes = new List<string>();

        public Action HeaderAction;

        public void ExtractTemplate(string file, Action<string> extractLine)
        {
            var stream = File.OpenText(file);

            // header
            var line = stream.ReadLine();
            ColumnIndexes.AddRange(line.Split(","));

            HeaderAction();

            // data
            line = stream.ReadLine();
            while (line != null)
            {
                extractLine(line);
                line = stream.ReadLine();
            }
        }

        public int FindIndex(string v)
        {
            for (int i = 0; i < ColumnIndexes.Count; i++)
            {
                if (ColumnIndexes[i] == v)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(v);
        }
    }
}
