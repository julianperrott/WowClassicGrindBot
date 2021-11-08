using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayerDirection
    {
        ValueTask SetDirection(float desiredDirection, Vector3 point, string source);

        ValueTask SetDirection(float desiredDirection, Vector3 point, string source, int ignoreDistance);

        DateTime LastSetDirection { get; }
    }
}