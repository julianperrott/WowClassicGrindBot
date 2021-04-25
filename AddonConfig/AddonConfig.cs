using System;
using System.IO;
using Newtonsoft.Json;

public static class AddonConfigVersion
{
    public static int Version = 1;
}

public class AddonConfig
{
    public int Version = AddonConfigVersion.Version;

    public string InstallPath;

    public string GameVersion;
    public string Author;
    public string Title;

    [NonSerialized]
    public const string DefaultFileName = "addon_config.json";

    public static AddonConfig Load()
    {
        if (Exists())
        {
            var loaded = JsonConvert.DeserializeObject<AddonConfig>(File.ReadAllText(DefaultFileName));
            if (loaded.Version == AddonConfigVersion.Version)
                return loaded;
        }

        return new AddonConfig().Save();
    }

    public static bool Exists()
    {
        return File.Exists(DefaultFileName);
    }

    private AddonConfig Save()
    {
        File.WriteAllText(DefaultFileName, JsonConvert.SerializeObject(this));
        return this;
    }
}