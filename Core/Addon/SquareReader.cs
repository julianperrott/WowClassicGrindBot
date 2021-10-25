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
        public int GetIntAtCell(int index)
        {
            return addonReader.GetIntAt(index);
        }

        // Converts a cell's hexidecimal color to a 6 point decimal
        public double GetFixedPointAtCell(int indexl)
        {
            return GetIntAtCell(indexl) / 100000f;
        }

        public string GetStringAtCell(int index)
        {
            var color = this.GetIntAtCell(index);
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
                var text = GetIntAtCell(index).ToString();
                if (text == "0")
                {
                    return 0;
                }

                var v1 = part switch
                {
                    Part.Right => text.Length-5 >= 0 ? int.Parse(text.Substring(text.Length - 5, 5)) : 0, // right 5 chars
                    Part.Left => text.Length-5 > 0 ? int.Parse(text.Substring(0, text.Length - 5)) : 0,  // left 5 chars
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