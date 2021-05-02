using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public interface IRouteProvider
    {
        List<WowPoint> PathingRoute();

        DateTime LastActive { get; set; }

        WowPoint? NextPoint();
    }
}
