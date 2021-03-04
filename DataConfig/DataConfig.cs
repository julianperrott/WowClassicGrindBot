using System;
using System.IO;
using Newtonsoft.Json;

public static class DataConfigVersion
{
    public static int Version = 2;
}

public class DataConfig
{
    public int Version = DataConfigVersion.Version;
    public string Root { get; } = "../json/";

    public string Class => System.IO.Path.Join(Root, "class/");
    public string Path => System.IO.Path.Join(Root, "path/");
    public string Data => System.IO.Path.Join(Root, "data/");
    public string WorldToMap => System.IO.Path.Join(Root, "WorldToMap/");
    public string PathInfo => System.IO.Path.Join(Root, "PathInfo/");
    public string MPQ => System.IO.Path.Join(Root, "MPQ/");

    [NonSerialized]
    public const string DefaultFileName = "data_config.json";

    public static DataConfig? FromJson()
    {
        if(File.Exists(DefaultFileName))
            return JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText(DefaultFileName));

        return null;
    }

    public DataConfig Save()
    {
        if (!File.Exists(DefaultFileName) || this.Version != DataConfigVersion.Version)
            File.WriteAllText(DefaultFileName, JsonConvert.SerializeObject(this));

        return this;
    }
}