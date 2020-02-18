using System.Drawing;

namespace Libs
{
    public interface ISquareReader
    {
        int Get5Numbers(DataFrame cell, SquareReader.Part part);
        double GetFixedPointAtCell(DataFrame cell);
        long GetLongAtCell(DataFrame cell);
        string GetStringAtCell(DataFrame cell);
    }
}