using System;

namespace Libs
{
    public class PixelClassifier : IPixelClassifier
    {
        public int MaxBlue { get; set; } = 34;

        public int MinRedGreen { get; set; } = 176;

        public bool IsMatch(byte red, byte green, byte blue)
        {
            return blue < MaxBlue && red > MinRedGreen && green > MinRedGreen;// && areClose(red, green);
        }

        private bool isBigger(byte red, byte other)
        {
            return (red * MaxBlue) > other;
        }

        private bool areClose(byte color1, byte color2)
        {
            var max = Math.Max(color1, color2);
            var min = Math.Min(color1, color2);

            return min * MinRedGreen > max - 20;
        }
    }
}