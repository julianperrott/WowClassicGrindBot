using System;

namespace Libs
{
    public interface IPlayerDirection
    {
        System.Threading.Tasks.Task SetDirection(double desiredDirection, WowPoint point, string source);

        DateTime LastSetDirection { get; }
    }
}