using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public interface IRouteProvider
    {
        List<WowPoint> PathingRoute();

        DateTime LastActive { get; set; }

        WowPoint? NextPoint();
    }
}
