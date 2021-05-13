using System.Collections.Generic;

namespace Core.Session
{
    public interface IGrindingSessionHandler
    {
        List<GrindingSession> Load();
        void Save(IGrindingSession grindingSession);
    }
}
