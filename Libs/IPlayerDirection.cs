using System;

namespace Libs
{
    public interface IPlayerDirection
    {
        void SetDirection(double desiredDirection);
        DateTime LastSetDirection { get; }
    }
}