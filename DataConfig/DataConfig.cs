using System;
using System.IO;
using Newtonsoft.Json;

public static class DataConfigVersion
{
    public static int Version = 6;
}

public class DataConfig
{
    public int Version = DataConfigVersion.Version;
    public string Root { get; set; } = "../json/";

    public string Class => System.IO.Path.Join(Root, "class/");
    public string Path => System.IO.Path.Join(Root, "path/");
    public string Dbc => System.IO.Path.Join(Root, "dbc/");
    public string WorldToMap => System.IO.Path.Join(Root, "WorldToMap/");
    public string PathInfo => System.IO.Path.Join(Root, "PathInfo/");
    public string MPQ => System.IO.Path.Join(Root, "MPQ/");
    public string Area => System.IO.Path.Join(Root, "area/");
    public string PPather => System.IO.Path.Join(Root, "PPather/");

    [NonSerialized]
    public const string DefaultFileName = "data_config.json";

    public static DataConfig Load()
    {
        if(File.Exists(DefaultFileName))
        {
            var loaded = JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText(DefaultFileName));
            if (loaded.Version == DataConfigVersion.Version)
                return loaded;
        }

        return new DataConfig().Save();
    }

    private DataConfig Save()
    {
        File.WriteAllText(DefaultFileName, JsonConvert.SerializeObject(this));

        return this;
    }
}