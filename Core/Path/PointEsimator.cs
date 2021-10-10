using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public static class PointEsimator
    {
        public const double YARD_TO_COORD = 0.035921;

        public static WowPoint GetPoint(WowPoint origo, double wowRad, double rangeYard)
        {
            //player direction
            //0.00061

            //player location
            //37.4017,44.4587

            //NPC
            //37.4016,44.2791

            //~5yard Distance
            //44.4587 - 44.2791 = 0.1796

            //~1yard Distance
            //0.1796 / 5 = 0.03592

            double range = rangeYard * YARD_TO_COORD;
            (double dirX, double dirY) = DirectionCalculator.ToNormalRadian(wowRad);

            return new WowPoint(origo.X + (range * dirX), origo.Y + (range * dirY), origo.Z);
        }
    }
}
