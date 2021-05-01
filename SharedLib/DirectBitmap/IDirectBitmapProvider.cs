using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib
{
    public interface IDirectBitmapProvider
    {
        DirectBitmap DirectBitmap { get; }
    }
}
