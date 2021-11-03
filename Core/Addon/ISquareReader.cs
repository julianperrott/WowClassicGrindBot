namespace Core
{
    public interface ISquareReader
    {
        double GetFixedPointAtCell(int indexl);

        int GetIntAtCell(int index);

        string GetStringAtCell(int index);
    }
}