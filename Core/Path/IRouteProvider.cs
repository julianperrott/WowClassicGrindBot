using System;
using System.Collections.Generic;
using System.Numerics;

namespace Core
{
    public interface IRouteProvider
    {
        List<Vector3> PathingRoute();

        DateTime LastActive { get; set; }

        bool HasNext();

        Vector3 NextPoint();
    }
}
