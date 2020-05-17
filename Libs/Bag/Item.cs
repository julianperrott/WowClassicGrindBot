using Microsoft.AspNetCore.Html;
using System;
using System.Text;

namespace Libs.Addon
{
    public class Item
    {
        public int Entry { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quality { get; set; }
        public int SellPrice { get; set; }

        public int[] ToSellPrice()
        {
            return ToSellPrice(SellPrice);
        }

        public static int[] ToSellPrice(int sellPrice)
        {
            if (sellPrice == 0) { return new int[3] { 0, 0, 0 }; }

            var sign = sellPrice < 0 ? -1 : 1;

            int gold = 0;
            int silver = 0;
            int copper = 0;

            var value = Math.Abs(sellPrice);

            if (value >= 10000)
            {
                gold = value / 10000;
                value = value % 10000;
            }

            if (value >= 100)
            {
                silver = value / 100;
                value = value % 100;
            }

            copper = value;

            return new int[3] { sign* gold, sign * silver, sign * copper };
        }
    }
}