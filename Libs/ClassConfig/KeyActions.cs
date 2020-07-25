using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Libs
{
    public class KeyActions
    {
        public List<KeyAction> Sequence { get; } = new List<KeyAction>();

        public void Initialise(PlayerReader playerReader, RequirementFactory requirementFactory, ILogger logger)
        {
            Sequence.ForEach(i => i.Initialise(playerReader, requirementFactory, logger));
        }
    }
}