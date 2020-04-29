using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Libs
{
    public class KeyConfigurations
    {
        public List<KeyConfiguration> Sequence { get; set; } = new List<KeyConfiguration>();

        public void Initialise(RequirementFactory requirementFactory, ILogger logger)
        {
            Sequence.ForEach(i => i.Initialise(requirementFactory, logger));
        }
    }
}
