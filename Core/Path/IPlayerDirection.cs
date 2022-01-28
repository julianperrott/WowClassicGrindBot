using System;
using System.Numerics;

namespace Core
{
    public interface IPlayerDirection
    {
        void SetDirection(float desiredDirection, Vector3 point, string source);

        void SetDirection(float desiredDirection, Vector3 point, string source, int ignoreDistance);

        DateTime LastSetDirection { get; }
    }
}