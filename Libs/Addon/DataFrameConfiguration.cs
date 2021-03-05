using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Libs
{
    public class DataFrameConfiguration
    {
        private class DataFrameConfig
        {
            public Rectangle rect;
            public List<DataFrame> frames = new List<DataFrame>();
        }

        private readonly IColorReader colorReader;

        public DataFrameConfiguration(IColorReader colorReader)
        {
            this.colorReader = colorReader;
        }

        private const string ConfigurationFilename = "frame_config.json";

        public static bool ConfigurationExists()
        {
            return File.Exists(ConfigurationFilename);
        }

        public static bool IsValid(Rectangle rect)
        {
            if (!ConfigurationExists()) return false;

            var config = JsonConvert.DeserializeObject<DataFrameConfig>(File.ReadAllText(ConfigurationFilename));
            return config.rect.Width == rect.Width && config.rect.Height == rect.Height;
        }

        public static List<DataFrame> LoadConfiguration()
        {
            var config = JsonConvert.DeserializeObject<DataFrameConfig>(File.ReadAllText(ConfigurationFilename));
            return config.frames;
        }

        public static void SaveConfiguration(Rectangle rect, List<DataFrame> dataFrames)
        {
            var config = new DataFrameConfig
            {
                rect = rect,
                frames = dataFrames
            };
            string output = JsonConvert.SerializeObject(config);

            File.WriteAllText(ConfigurationFilename, output);
        }

        public static void RemoveConfiguration()
        {
            if(ConfigurationExists())
            {
                File.Delete(ConfigurationFilename);
            }
        }

        public List<DataFrame> CreateConfiguration(Bitmap bmp)
        {
            var dataFrames = new List<DataFrame>() { new DataFrame(new Point(1, 1), 0) };
            for (int dataframe = 1; dataframe < 400; dataframe++)
            {
                var point = GetFramePoint(bmp, dataframe, dataFrames.Last().point.X);
                if (point == null)
                {
                    break;
                }
                dataFrames.Add(new DataFrame(point.Value, dataframe));
            }

            return dataFrames;
        }

        private Point? GetFramePoint(Bitmap bmp, int dataframe, int startX)
        {
            for (int x = startX; x < bmp.Width; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    var point = new Point(x, y);
                    if (colorReader.GetColorAt(point, bmp).B == dataframe)
                    {
                        return point;
                    }
                }
            }

            return null;
        }
    }
}