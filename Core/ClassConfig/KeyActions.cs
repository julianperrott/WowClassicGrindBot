using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Core
{
    public class KeyActions
    {
        public List<KeyAction> Sequence { get; } = new List<KeyAction>();

        public void PreInitialise(string prefix, RequirementFactory requirementFactory, ILogger logger)
        {
            if (Sequence.Count > 0)
            {
                logger.LogInformation($"[{prefix}] CreateDynamicBindings.");
            }

            Sequence.ForEach(i => i.CreateDynamicBinding(requirementFactory));
        }

        public void Initialise(string prefix, AddonReader addonReader, RequirementFactory requirementFactory, ILogger logger)
        {
            if (Sequence.Count > 0)
            {
                logger.LogInformation($"[{prefix}] Initialise KeyActions.");
            }

            Sequence.ForEach(i => i.Initialise(addonReader, requirementFactory, logger, this));
        }
    }
}