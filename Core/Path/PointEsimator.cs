using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Core
{
    public static class PointEsimator
    {
        public const float YARD_TO_COORD = 0.035921f;

        public static Vector3 GetPoint(Vector3 origo, float wowRad, float rangeYard)
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

            float range = rangeYard * YARD_TO_COORD;
            (float dirX, float dirY) = DirectionCalculator.ToNormalRadian(wowRad);

            return new Vector3(origo.X + (range * dirX), origo.Y + (range * dirY), origo.Z);
        }
    }
}
