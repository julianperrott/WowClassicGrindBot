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

    public string InstallPath { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }

    public string Command { get; set; }

    public bool IsDefault()
    {
        return string.IsNullOrEmpty(InstallPath) || 
            string.IsNullOrEmpty(Author) || 
            string.IsNullOrEmpty(Title);
    }

    [NonSerialized]
    public const string DefaultFileName = "addon_config.json";

    private AddonConfig() { }

    public static AddonConfig Load()
    {
        if (Exists())
        {
            var loaded = JsonConvert.DeserializeObject<AddonConfig>(File.ReadAllText(DefaultFileName));
            if (loaded.Version == AddonConfigVersion.Version)
                return loaded;
        }

        return new AddonConfig();
    }

    public static bool Exists()
    {
        return File.Exists(DefaultFileName);
    }

    public static void Delete()
    {
        if (Exists())
        {
            File.Delete(DefaultFileName);
        }
    }

    public void Save()
    {
        File.WriteAllText(DefaultFileName, JsonConvert.SerializeObject(this));
    }
}