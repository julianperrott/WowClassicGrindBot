namespace Libs.Addon
{
    public class Item
    {
        public int Entry { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quality { get; set; }
        public int SellPrice { get; set; }

        public string ToSellPrice()
        {
            if (SellPrice >= 10000)
            {
                return (((double)SellPrice) / 10000).ToString("0.0") + "g";
            }

            return (((double)SellPrice) / 100).ToString("0.0") + "s";
        }
    }
}