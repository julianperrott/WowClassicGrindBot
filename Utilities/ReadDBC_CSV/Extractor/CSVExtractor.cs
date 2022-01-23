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

        public static string[] SplitQuotes(string csvText)
        {
            List<string> tokens = new List<string>();

            int last = -1;
            int current = 0;
            bool inText = false;

            while (current < csvText.Length)
            {
                switch (csvText[current])
                {
                    case '"':
                        inText = !inText; break;
                    case ',':
                        if (!inText)
                        {
                            tokens.Add(csvText.Substring(last + 1, (current - last)).Trim(' ', ','));
                            last = current;
                        }
                        break;
                    default:
                        break;
                }
                current++;
            }

            if (last != csvText.Length - 1)
            {
                tokens.Add(csvText.Substring(last + 1).Trim());
            }

            return tokens.ToArray();
        }
    }
}
