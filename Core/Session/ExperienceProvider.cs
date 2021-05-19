using System.IO;
using Newtonsoft.Json;

namespace Core.Session
{
    public static class ExperienceProvider
    {
        public static double[] GetExperienceList()
        {
            var dataConfig = new DataConfig();
            var json = File.ReadAllText($"{dataConfig.Experience}exp.json");
            var expList = JsonConvert.DeserializeObject<double[]>(json);
            return expList;
        }
    }
}
