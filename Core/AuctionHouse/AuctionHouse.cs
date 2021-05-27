using Core.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core
{
    public class AuctionHouse
    {
        private readonly ILogger logger;
        private readonly DataConfig dataConfig;
        private readonly ItemDB itemDb;

        public AuctionHouse(ILogger logger, DataConfig dataConfig, ItemDB itemDb)
        {
            this.logger = logger;
            this.dataConfig = dataConfig;
            this.itemDb = itemDb;
        }

        public Dictionary<int, int> ReadAHPrices()
        {
            var auctionHouseDictionary = new Dictionary<int, int>();

            // \WTF\Account\XXXXXXX\SavedVariables\Auc-Stat-Simple.lua
            string aucStatSimple = "Auc-Stat-Simple.lua";
            if (File.Exists(Path.Join(dataConfig.AuctionHouse, aucStatSimple)))
            {
                string path = Path.Join(dataConfig.AuctionHouse, aucStatSimple);
                try
                {
                    File.ReadAllLines(path)
                        .Select(l => l.Trim())
                        .Where(l => l.StartsWith("["))
                        .Where(l => l.Contains(";"))
                        .Where(l => l.Contains("="))
                        .Select(AucStatSimple_Process)
                        .GroupBy(l => l.Key)
                        .Select(g => g.First())
                        .ToList()
                        .ForEach(i => auctionHouseDictionary.Add(i.Key, i.Value));

                    logger.LogInformation($"Found {aucStatSimple} with {auctionHouseDictionary.Count} entries.");
                    return auctionHouseDictionary;
                }
                catch (Exception ex)
                {
                    logger.LogError($"{aucStatSimple} Failed to read AH prices." + ex.Message);
                }
            }

            // \WTF\Account\XXXXXXX\SavedVariables\Auctionator.lua
            string auctionator = "Auctionator.lua";
            if (File.Exists(Path.Join(dataConfig.AuctionHouse, auctionator)))
            {
                string path = Path.Join(dataConfig.AuctionHouse, auctionator);
                try
                {
                    auctionHouseDictionary = Auctionator_Process(File.ReadAllLines(path));
                    logger.LogInformation($"Found {auctionator} with {auctionHouseDictionary.Count} entries.");
                    return auctionHouseDictionary;
                }
                catch (Exception ex)
                {
                    logger.LogError($"{auctionator} Failed to read AH prices." + ex.Message);
                }
            }

            logger.LogWarning("AH prices unavailable!");
            return auctionHouseDictionary;
        }

        #region Auc-Stat-Simple.lua

        private static KeyValuePair<int, int> AucStatSimple_Process(string line)
        {
            var parts = line.Split("=");
            var id = int.Parse(parts[0].Replace("[", "").Replace("]", "").Replace("\"", "").Trim());
            var valueParts = parts[1].Split("\"")[1].Split(";");

            double value = 0;
            if (valueParts.Length == 3 || valueParts.Length == 6)
            {
                value = double.Parse(valueParts[valueParts.Length - 1]);
            }
            else if (valueParts.Length == 4 || valueParts.Length == 7)
            {
                value = double.Parse(valueParts[valueParts.Length - 2]);
            }

            return new KeyValuePair<int, int>(id, (int)value);
        }

        #endregion

        #region Auctionator.lua

        private Dictionary<int, int> Auctionator_Process(string[] lines)
        {
            var priceDict = new Dictionary<int, int>();

            int startPriceDatabaseIndex = -1;
            string serverName = string.Empty;
            string currentItemName = string.Empty;

            Dictionary<string, string> childProp = new Dictionary<string, string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("AUCTIONATOR_PRICE_DATABASE = {"))
                {
                    // start of AUCTIONATOR_PRICE_DATABASE
                    startPriceDatabaseIndex = i;
                }
                else if (startPriceDatabaseIndex != -1)
                {
                    if(lines[i].StartsWith("}"))
                    {
                        // end of AUCTIONATOR_PRICE_DATABASE
                        break;
                    }

                    else if (string.IsNullOrEmpty(serverName) && 
                        lines[i].StartsWith("\t") && lines[i].EndsWith("{"))
                    {
                        // first server
                        serverName = lines[i].Split("\"")[1];
                        logger.LogInformation($"Auctionator found {serverName}");
                    }
                    else
                    {
                        if (lines[i].StartsWith("\t\t") && string.IsNullOrEmpty(currentItemName))
                        {
                            // item start
                            currentItemName = lines[i].Split("\"")[1];
                        }
                        else if (lines[i].StartsWith("\t\t") && lines[i].EndsWith("},"))
                        {
                            // item end
                            // ah price
                            // like this H3846 -> 1315
                            var priceKey = childProp.Keys.Where(x => x.StartsWith("H")).FirstOrDefault();
                            if (priceKey != null)
                            {
                                int price = int.Parse(childProp[priceKey]);

                                var item = itemDb.Get(currentItemName);
                                if (item != null)
                                {
                                    if (!priceDict.ContainsKey(item.Entry))
                                        priceDict.Add(item.Entry, price);
                                }
                            }

                            currentItemName = string.Empty;
                            childProp.Clear();
                            continue;
                        }
                        else if (!string.IsNullOrEmpty(currentItemName) && lines[i].StartsWith("\t\t\t"))
                        {
                            // child properties
                            //like ["H3846"] = 57230,
                            string[] kvp = lines[i].Replace("\t", "").Split("=");
                            string key = kvp[0].Replace("\"", "").Replace("[", "").Replace("]", "");
                            string value = kvp[1].Replace(",", "");
                            childProp.Add(key, value);
                        }
                    }
                }
            }

            return priceDict;
        }

        #endregion
    }
}
