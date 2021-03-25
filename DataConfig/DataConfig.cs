using System;
using System.IO;
using Newtonsoft.Json;

public static class DataConfigVersion
{
    public static int Version = 7;
}

public class DataConfig
{
    public int Version = DataConfigVersion.Version;
    public string Root { get; set; } = "../json/";

    [JsonIgnore]
    public string Class => System.IO.Path.Join(Root, "class/");
    [JsonIgnore]
    public string Path => System.IO.Path.Join(Root, "path/");
    [JsonIgnore]
    public string Dbc => System.IO.Path.Join(Root, "dbc/");
    [JsonIgnore]
    public string WorldToMap => System.IO.Path.Join(Root, "WorldToMap/");
    [JsonIgnore]
    public string PathInfo => System.IO.Path.Join(Root, "PathInfo/");
    [JsonIgnore]
    public string MPQ => System.IO.Path.Join(Root, "MPQ/");
    [JsonIgnore]
    public string Area => System.IO.Path.Join(Root, "area/");
    [JsonIgnore]
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