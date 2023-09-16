using Helpers;

namespace Chess;

public struct Square
{

    public static Square[] directions = new Square[] { new Square(1, 0), new Square(-1, 0), new Square(0, 1), new Square(0, -1), new Square(1, 1), new Square(-1, 1), new Square(1, -1), new Square(-1, -1) };
    public static Square[] rookDirections => directions[0..4];
    public static Square[] bishopDirections => directions[4..8];
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

    public bool IsHorizontal => rank == 0;
    public bool IsVertical => file == 0;
    public bool IsDiagonal => file - rank == 0;
    public bool IsAntiDiagonal => file + rank == 0;

    public static Square operator +(Square a, Square b) => new Square(a.file + b.file, a.rank + b.rank);
    public static Square operator -(Square a, Square b) => new Square(a.file - b.file, a.rank - b.rank);
    public static Square operator *(Square a, int b) => new Square(a.file * b, a.rank * b);
}