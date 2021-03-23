using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WowheadDB;

namespace WowheadDB_Extractor
{
    class Program
    {
        private const string outputPath = "../../../../../Json/area/";
        private const string ZONE_URL = "https://classic.wowhead.com/zone=";

        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async static Task MainAsync(string[] args)
        {
            await ExtractZones();
        }

        static async Task ExtractZones()
        {
            foreach (KeyValuePair<string, int> entry in Areas.Classic)
            {
                if (entry.Value == 0) continue;
                try
                {
                    var p = GetPayloadFromWebpage(await LoadPage(entry.Value));
                    var z = ZoneFromJson(p);
                    SaveZone(z, entry.Value.ToString());

                    Console.WriteLine($"Saved {entry.Value,5}={entry.Key}");
                }
                catch { }

                await Task.Delay(50);
            }
        }

        static async Task<string> LoadPage(int zoneId)
        {
            var url = ZONE_URL + zoneId;

            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        static string GetPayloadFromWebpage(string content)
        {
            string beginPat = "<script>var mapShower = new ShowOnMap(";
            string endPat = ");</script>";

            int beginPos = content.IndexOf(beginPat);
            int endPos = content.IndexOf(endPat, beginPos);

            return content.Substring(beginPos + beginPat.Length, endPos - beginPos - beginPat.Length);
        }

        static Area ZoneFromJson(string content)
        {
            return JsonConvert.DeserializeObject<Area>(content);
        }

        static void SaveZone(Area zone, string name)
        {
            var output = JsonConvert.SerializeObject(zone);
            var file = Path.Join(outputPath, name  + ".json");

            File.WriteAllText(file, output);
        }


        #region local tests

        static void SerializeTest()
        {
            int zoneId = 40;
            var file = Path.Join(outputPath, zoneId + ".json");
            var zone = ZoneFromJson(File.ReadAllText(file));
        }

        static void ExtractFromFileTest()
        {
            var file = Path.Join(outputPath, "a.html");
            var html = File.ReadAllText(file);

            string payload = GetPayloadFromWebpage(html);
            var zone = ZoneFromJson(payload);
        }

        #endregion
    }
}