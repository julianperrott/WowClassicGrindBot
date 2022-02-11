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
        public float GetFixedPointAtCell(int index)
        {
            return GetIntAtCell(index) / 100000f;
        }

        public string GetStringAtCell(int index)
        {
            int color = GetIntAtCell(index);
            if (color != 0)
            {
                string colorString = color.ToString();
                if (colorString.Length > 6) { return string.Empty; }
                string colorText = "000000"[..(6 - colorString.Length)] + colorString;
                return ToChar(colorText, 0) + ToChar(colorText, 2) + ToChar(colorText, 4);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string ToChar(string colorText, int start)
        {
            return ((char)int.Parse(colorText.Substring(start, 2))).ToString();
        }
    }
}