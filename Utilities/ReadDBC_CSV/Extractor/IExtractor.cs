using System.Collections.Generic;

namespace ReadDBC_CSV
{
    public interface IExtractor
    {
        public List<string> FileRequirement { get; }

        void Run();
    }
}
