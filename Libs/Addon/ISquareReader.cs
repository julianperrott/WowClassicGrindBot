using System.Drawing;

namespace Libs
{
    public interface ISquareReader
    {
        int Get5Numbers(int index, SquareReader.Part part);
        double GetFixedPointAtCell(int indexl);
        long GetLongAtCell(int index);
        string GetStringAtCell(int index);
    }
}