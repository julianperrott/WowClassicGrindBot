using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

#nullable enable

namespace SharedLib
{
    public static class DataFrameConfigurationVersion
    {
        public const int Version = 2;
    }

    public class DataFrameConfiguration
    {
        private class DataFrameConfig
        {
            public int Version = DataFrameConfigurationVersion.Version;
            public System.Version? addonVersion;
            public Rectangle rect;
            public DataFrameMeta meta;
            public List<DataFrame> frames = new List<DataFrame>();
        }

        private readonly IColorReader colorReader;

        public DataFrameConfiguration(IColorReader colorReader)
        {
            this.colorReader = colorReader;
        }

        private const string ConfigurationFilename = "frame_config.json";

        public static bool Exists()
        {
            return File.Exists(ConfigurationFilename);
        }

        public static bool IsValid(Rectangle rect, System.Version? addonVersion)
        {
            if (!Exists()) return false;

            var config = JsonConvert.DeserializeObject<DataFrameConfig>(File.ReadAllText(ConfigurationFilename));
            var sameVersion = config.Version == DataFrameConfigurationVersion.Version;
            var sameAddonVersion = addonVersion != null && addonVersion == config.addonVersion;
            var sameRect = config.rect.Width == rect.Width && config.rect.Height == rect.Height;
            return sameAddonVersion && sameVersion && sameRect && config.frames.Count > 1;
        }

        public static List<DataFrame> LoadFrames()
        {
            var config = JsonConvert.DeserializeObject<DataFrameConfig>(File.ReadAllText(ConfigurationFilename));
            if(config.Version == DataFrameConfigurationVersion.Version)
                return config.frames;

            return new List<DataFrame>();
        }

        public static DataFrameMeta LoadMeta()
        {
            var config = JsonConvert.DeserializeObject<DataFrameConfig>(File.ReadAllText(ConfigurationFilename));
            if (config.Version == DataFrameConfigurationVersion.Version)
                return config.meta;

            return DataFrameMeta.Empty;
        }

        public static void SaveConfiguration(Rectangle rect, System.Version addonVersion, DataFrameMeta meta, List<DataFrame> dataFrames)
        {
            var config = new DataFrameConfig
            {
                rect = rect,
                addonVersion = addonVersion,
                meta = meta,
                frames = dataFrames
            };
            string output = JsonConvert.SerializeObject(config);

            File.WriteAllText(ConfigurationFilename, output);
        }

        public static void RemoveConfiguration()
        {
            if(Exists())
            {
                File.Delete(ConfigurationFilename);
            }
        }

        public static DataFrameMeta GetMeta(Bitmap bmp)
        {
            var color = bmp.GetPixel(0, 0);
            int data, hash;
            data = hash = color.R * 65536 + color.G * 256 + color.B;

            if (hash == 0)
                return DataFrameMeta.Empty;

            // CELL_SPACING * 10000000 + CELL_SIZE * 100000 + 1000 * FRAME_ROWS + NUMBER_OF_FRAMES
            int spacing = (int)(data / 10000000f);
            data -= (10000000 * spacing);

            int size = (int)(data / 100000f);
            data -= (100000 * size);

            int rows = (int)(data / 1000f);
            data -= (1000 * rows);

            int count = data;

            return new DataFrameMeta(hash, spacing, size, rows, count);
        }

        public static List<DataFrame> CreateFrames(DataFrameMeta meta, Bitmap bmp)
        {
            var dataFrames = new List<DataFrame>() { new DataFrame(new Point(0, 0), 0) };
            for (int dataframe = 1; dataframe < meta.frames; dataframe++)
            {
                var point = GetFramePoint(meta, bmp, dataframe, dataFrames.Last().point.X);
                if (point == null)
                {
                    break;
                }
                dataFrames.Add(new DataFrame(point.Value, dataframe));
            }

            return dataFrames;
        }

        private static Point? GetFramePoint(DataFrameMeta meta, Bitmap bmp, int dataframe, int startX)
        {
            for (int x = startX; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    if (bmp.GetPixel(x, y).B == dataframe)
                    {
                        if (meta.size > 1 && x + 1 < bmp.Width && y + 1 < bmp.Height && bmp.GetPixel(x + 1, y + 1).B == dataframe)
                            return new Point(x + 1, y + 1);
                        else
                            return new Point(x, y);
                    }
                }
            }

            return null;
        }
    }
}