using System.IO;
using Newtonsoft.Json;

namespace Core.Session
{
    public static class ExperienceProvider
    {
        public static float[] GetExperienceList()
        {
            var dataConfig = new DataConfig();
            var json = File.ReadAllText($"{dataConfig.Experience}exp_tbc.json");
            var expList = JsonConvert.DeserializeObject<float[]>(json);
            return expList;
        }
    }
}
