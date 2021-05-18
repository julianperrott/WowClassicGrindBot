using System.Collections.Generic;

namespace Core.Session
{
    public interface IGrindSessionHandler
    {
        List<GrindSession> Load();
        void Save(IGrindSession grindSession);
    }
}
