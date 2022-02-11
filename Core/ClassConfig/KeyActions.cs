using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Core
{
    public partial class KeyActions
    {
        public List<KeyAction> Sequence { get; } = new List<KeyAction>();

        public void PreInitialise(string prefix, RequirementFactory requirementFactory, ILogger logger)
        {
            if (Sequence.Count > 0)
            {
                LogDynamicBinding(logger, prefix);
            }

            Sequence.ForEach(i => i.CreateDynamicBinding(requirementFactory));
        }

        public void Initialise(string prefix, AddonReader addonReader, RequirementFactory requirementFactory, ILogger logger)
        {
            if (Sequence.Count > 0)
            {
                LogInitKeyActions(logger, prefix);
            }

            Sequence.ForEach(i => i.Initialise(addonReader, requirementFactory, logger, this));
        }

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "[{prefix}] CreateDynamicBindings.")]
        static partial void LogDynamicBinding(ILogger logger, string prefix);

        [LoggerMessage(
            EventId = 11,
            Level = LogLevel.Information,
            Message = "[{prefix}] Initialise KeyActions.")]
        static partial void LogInitKeyActions(ILogger logger, string prefix);

    }
}