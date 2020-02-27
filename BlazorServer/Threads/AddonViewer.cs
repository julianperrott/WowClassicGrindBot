using Libs;
using System;
using System.Drawing;
using System.Threading;

namespace BlazorServer.Threads
{
    public class AddonViewer
    {
        private Random random = new Random();

        public StaticAddonReader AddonReader { get; private set; }
        private readonly ISquareReader squareReader;
        public static PlayerReader PlayerReader { get; private set; }
        //public BagReader bagReader { get; private set; }
        //public EquipmentReader equipmentReader { get; private set; }

        public static event EventHandler AddonDataChanged;

        public void DoWork(object obj)
        {
            while (true)
            {
                Thread.Sleep(1000);

                var frames = new Color[10];
                for (int i = 0; i < 10; i++)
                {
                    frames[i] = Color.FromArgb(random.Next(100));
                }

                AddonReader.Refresh(frames);

                // update UI.
                var args = new EventArgs();
                AddonDataChanged?.Invoke(AddonReader, args);
            }
        }

        public AddonViewer()
        {
            this.AddonReader = new StaticAddonReader();
            this.squareReader = new SquareReader(AddonReader);

            //this.bagReader = new BagReader(squareReader, 20);
            //this.equipmentReader = new EquipmentReader(squareReader, 30);
            PlayerReader = new PlayerReader(squareReader);
        }
    }
}