namespace Core
{
    public interface ISquareReader
    {
        float GetFixedPointAtCell(int indexl);

        int GetIntAtCell(int index);

        string GetStringAtCell(int index);
    }
}