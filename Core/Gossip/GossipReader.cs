using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public class GossipReader
    {
        private readonly int cGossip;

        private readonly ISquareReader reader;

        private DateTime lastEvent;

        public int Count { private set; get; }
        public Dictionary<Gossip, int> Gossips { get; private set; } = new Dictionary<Gossip, int>();

        private int data;

        public bool Ready => Gossips.Count == Count;

        public bool GossipStart => data == 69;
        public bool GossipEnd => data == 9999994;

        public bool MerchantWindowOpened => data == 9999999;

        public bool MerchantWindowClosed => data == 9999998;

        public bool MerchantWindowSelling => data == 9999997;

        public bool MerchantWindowSellingFinished => data == 9999996;

        public GossipReader(ISquareReader reader, int cGossip)
        {
            this.reader = reader;
            this.cGossip = cGossip;
        }

        public void Read()
        {
            data = reader.GetIntAtCell(cGossip);

            // used for merchant window open state
            if (MerchantWindowClosed || MerchantWindowOpened || MerchantWindowSelling || MerchantWindowSellingFinished || GossipEnd)
                return;

            if (data == 0 || GossipStart)
            {
                Count = 0;
                Gossips.Clear();
                lastEvent = DateTime.UtcNow;

                return;
            }

            // formula
            // 10000 * count + 100 * index + value
            int count = (int)(data / 10000f);
            data -= 10000 * count;

            int order = (int)(data / 100f);
            data -= 100 * order;

            Count = count;

            if (!Gossips.ContainsKey((Gossip)data))
            {
                Gossips.Add((Gossip)data, order);
            }
        }

    }
}
