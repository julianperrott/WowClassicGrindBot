using System;
using System.Collections.Generic;
using System.Text;

namespace WowheadDB_Extractor
{
    public static class Areas
    {
        public static string GetUrl(string key)
        {
            _ = Classic.TryGetValue(key, out int link);
            return $"https://wow.zamimg.com/images/wow/classic/maps/enus/zoom/{link}.jpg";
        }

        public static Dictionary<string, int> Classic = new Dictionary<string, int>
        {
        // EK
        { " ---------------------- Eastern Kingdom ----------------------", 0 },

        { "Alterac Mountains", 36 },
        { "Arathi Highlands", 45 },
        { "Badlands", 3 },
        { "Blasted Lands", 4 },
        { "Burning Steppes", 46 },
        { "Deadwind Pass", 41 },
        { "Dun Morogh", 1 },
        { "Duskwood", 10 },
        { "Eastern Plaguelands", 139 },
        { "Elwynn Forest", 12 },
        { "Hillsbrad Foothills", 267 },
        { "Ironforge", 1537 },
        { "Loch Modan", 38 },
        { "Redridge Mountains", 44 },
        { "Searing Gorge", 51 },
        { "Silverpine Forest", 130 },
        { "Stormwind City", 1519 },
        { "Stranglethorn Vale", 33 },
        { "Swamp of Sorrows", 8 },
        { "The Hinterlands", 47 },
        { "Tirisfal Glades", 85 },
        { "Undercity", 1497 },
        { "Western Plaguelands", 28 },
        { "Westfall", 40 },
        { "Wetlands", 11 },

        // Kalimdor
        { " ---------------------- Kalimdor ----------------------", 0 },

        { "Ashenvale", 331 },
        { "Azshara", 16 },
        { "Darkshore", 148 },
        { "Darnassus", 1657 },
        { "Desolace", 405 },
        { "Durotar", 14 },
        { "Dustwallow Marsh", 15 },
        { "Felwood", 361 },
        { "Feralas", 357 },
        { "Moonglade", 493 },
        { "Mulgore", 215 },
        { "Orgrimmar", 1637 },
        { "Silithus", 1377 },
        { "Stonetalon Mountains", 406 },
        { "Tanaris", 440 },
        { "Teldrassil", 141 },
        { "The Barrens", 17 },
        { "Thousand Needles", 400 },
        { "Thunder Bluff", 1638 },
        { "Un'Goro Crater", 490 },
        { "Winterspring", 618 }
    };
    }
}