using System;
using System.Threading.Tasks;

namespace Libs
{
    public interface IPlayerDirection
    {
        Task SetDirection(double desiredDirection, WowPoint point, string source);

        Task SetDirection(double desiredDirection, WowPoint point, string source, int ignoreDistance);

        DateTime LastSetDirection { get; }
    }
}