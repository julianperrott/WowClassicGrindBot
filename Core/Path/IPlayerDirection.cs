using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Core
{
    public interface IPlayerDirection
    {
        Task SetDirection(float desiredDirection, Vector3 point, string source);

        Task SetDirection(float desiredDirection, Vector3 point, string source, int ignoreDistance);

        DateTime LastSetDirection { get; }
    }
}