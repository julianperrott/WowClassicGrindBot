using Microsoft.Extensions.Logging;
using System;

namespace Libs
{
    public class DirectionCalculator
    {
        private ILogger logger;

        public DirectionCalculator(ILogger logger)
        {
            this.logger = logger;
        }

        public double CalculateHeading(WowPoint from, WowPoint to)
        {
            //logger.LogInformation($"from: ({from.X},{from.Y}) to: ({to.X},{to.Y})");

            var target = Math.Atan2(to.X - from.X, to.Y - from.Y);
            return Math.PI + target;
        }
    }
}