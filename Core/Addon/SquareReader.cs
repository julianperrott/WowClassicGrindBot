namespace Core
{
    public class SquareReader : ISquareReader
    {
        private readonly IAddonReader addonReader;

        public SquareReader(IAddonReader addonReader)
        {
            this.addonReader = addonReader;
        }

        // Converts a cell's hexideciml color code to decimal data
        public long GetLongAtCell(int index)
        {
            // Finding the hexidecimal color
            var color = addonReader.GetColorAt(index);
            // Converting from base 16 (hexidecimal) to base 10 (decimal)
            return color.R * 65536 + color.G * 256 + color.B;
        }

        // Converts a cell's hexidecimal color to a 6 point decimal
        public double GetFixedPointAtCell(int indexl)
        {
            return (double)this.GetLongAtCell(indexl) / 100000;
        }

        public string GetStringAtCell(int index)
        {
            var color = this.GetLongAtCell(index);
            if (color != 0)
            {
                var colorString = color.ToString();
                if (colorString.Length > 6) { return string.Empty; }
                var colorText = "000000".Substring(0, 6 - colorString.Length) + colorString;
                return ToChar(colorText, 0) + ToChar(colorText, 2) + ToChar(colorText, 4);
            }
            else
            {
                return string.Empty;
            }
        }

        public enum Part
        {
            Right,
            Left
        }

        public int Get5Numbers(int index, Part part)
        {
            try
            {
                var text = GetLongAtCell(index).ToString();
                if (text == "0")
                {
                    return 0;
                }

                var v1 = part switch
                {
                    Part.Right => int.Parse(text.Substring(text.Length - 5, 5)), // right 5 chars
                    Part.Left => int.Parse(text.Substring(0, text.Length - 5)),// left 5 chars
                    _ => 0
                };

                return v1;
            }
            catch
            {
                return 0;
            }
        }

        private string ToChar(string colorText, int start)
        {
            return ((char)(int.Parse(colorText.Substring(start, 2)))).ToString();
        }
    }
}