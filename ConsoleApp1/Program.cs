using FishingFun;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{


  


    class Program
    {
        

        static void Main(string[] args)
        {
            var dataFrames = LoadConfiguration();

            if (dataFrames == null)
            {
                dataFrames = CreateConfiguration();
                SaveConfiguration(dataFrames);
            }

            var width = dataFrames.Last().point.X+1;
            var height = dataFrames.Max(f => f.point.Y)+1;

            while (true)
            {
                System.Threading.Thread.Sleep(100);
                var bmp = WowScreen.GetAddonBitmap(width, height);

                var color = WowScreen.GetColorAt(dataFrames[1].point, bmp);

                var squareReader = new SquareReader(bmp);
                var xcoord = squareReader.GetFixedPointAtCell(dataFrames[1]) * 10;
                var ycoord = squareReader.GetFixedPointAtCell(dataFrames[2]) * 10;
                var direction = squareReader.GetFixedPointAtCell(dataFrames[3]);

                System.Diagnostics.Debug.WriteLine($"X: {xcoord}, Y: {ycoord}, Direction: {direction}");
            }
        }


    }
}

