using System;
using System.Drawing;

namespace Core
{
    public interface IAddonDataProvider : IDisposable
    {
        void Update();
        Color GetColor(int index);
        int GetInt(int index);
    }
}
