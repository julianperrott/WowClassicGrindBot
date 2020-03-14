using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Libs
{
    public class StaticAddonReader : IAddonReader
    {
        public PlayerReader? PlayerReader { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private Color[] FrameColor { get; set; } = new Color[100];

        public Color GetColorAt(int index)
        {
            if (index>= FrameColor.Length) { return Color.Black; }
            return FrameColor[index];
        }

        public void Refresh(Color[] frameColor)
        {
            this.FrameColor = frameColor;
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }
    }
}
