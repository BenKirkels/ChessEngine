namespace Chess;

public struct Square
{
    public int file;
    public int rank;

    public int index => BoardHelper.FileRankToIndex(file, rank);

    public Square(int file, int rank)
    {
        this.file = file;
        this.rank = rank;
    }
    public Square(int index)
    {
        file = BoardHelper.IndexToFile(index);
        rank = BoardHelper.IndexToRank(index);
    }

    public bool IsValid => file >= 0 && file < 8 && rank >= 0 && rank < 8;

    public static Square operator +(Square a, Square b) => new Square(a.file + b.file, a.rank + b.rank);
    public static Square operator *(Square a, int b) => new Square(a.file * b, a.rank * b);
}