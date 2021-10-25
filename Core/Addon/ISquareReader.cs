namespace Core
{
    public interface ISquareReader
    {
        int Get5Numbers(int index, SquareReader.Part part);

        double GetFixedPointAtCell(int indexl);

        int GetIntAtCell(int index);

        string GetStringAtCell(int index);
    }
}