using System;

namespace Core
{
    public interface IAddonDataProvider : IDisposable
    {
        void Update();
        int GetInt(int index);
    }
}
