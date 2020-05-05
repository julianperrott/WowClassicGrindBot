using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Libs
{
    public class KeyConfigurations
    {
        public List<KeyConfiguration> Sequence { get; set; } = new List<KeyConfiguration>();

        public void Initialise(PlayerReader playerReader, RequirementFactory requirementFactory, ILogger logger)
        {
            Sequence.ForEach(i => i.Initialise(playerReader, requirementFactory, logger));
        }
    }
}