using System;
using System.IO;
using System.Net;

namespace ReadDBC_CSV
{
    class Program
    {
        static string userAgent = " Mozilla/5.0 (Windows NT 6.1; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0";

        static string path = "../../../data/";

        static string game_build = "2.5.2.40617";
        static string wma_build = "2.4.3.8606";

        static void Main(string[] args)
        {
            GenerateItems(path);

            GenerateConsumables(path);
            GenerateSpells(path);
            GenerateTalents(path);
            GenerateWorldMapArea(path);
        }

        private static void GenerateItems(string path)
        {
            var extractor = new ItemExtractor(path);
            DownloadRequirements(path, extractor, game_build);
            extractor.Run();
        }

        private static void GenerateConsumables(string path)
        {
            string foodDesc = "Restores $o1 health over $d";
            string waterDesc = "mana over $d"; // classic: Restores $o1 mana over $d

            var extractor = new ConsumablesExtractor(path, foodDesc, waterDesc);
            DownloadRequirements(path, extractor, game_build);
            extractor.Run();
        }

        private static void GenerateSpells(string path)
        {
            var extractor = new SpellExtractor(path);
            DownloadRequirements(path, extractor, game_build);
            extractor.Run();
        }

        private static void GenerateTalents(string path)
        {
            var extractor = new TalentExtractor(path);
            DownloadRequirements(path, extractor, game_build);
            extractor.Run();
        }

        private static void GenerateWorldMapArea(string path)
        {
            var extractor = new WorldMapAreaExtractor(path, wma_build);
            DownloadRequirements(path, extractor, wma_build, game_build);
            extractor.Run();
        }


        #region Download files

        private static void DownloadRequirements(string path, IExtractor extractor, params string[] builds)
        {
            foreach(var file in extractor.FileRequirement)
            {
                if (File.Exists(Path.Join(path, file)))
                {
                    Console.WriteLine($"{file} already exists. Skip downloading.");
                    continue;
                }

                foreach (var build in builds)
                {
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("user-agent", userAgent);

                        try
                        {
                            string url = DownloadURL(build, file);
                            client.DownloadFile(url, Path.Join(path, file));

                            Console.WriteLine($"{file} - {build} - Downloaded");

                            break;
                        }
                        catch (Exception e)
                        {
                            if (File.Exists(Path.Join(path, file)))
                            {
                                File.Delete(Path.Join(path, file));
                            }

                            Console.WriteLine($"{file} - {build} - {e.Message}");
                        }
                    }
                }
            }
        }

        private static string DownloadURL(string build, string file)
        {
            string resource = file.Split(".")[0];
            return $"https://wow.tools/dbc/api/export/?name={resource}&build={build}";
        }

        #endregion
    }
}
