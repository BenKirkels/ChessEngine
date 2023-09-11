namespace Chess;
public static class BoardHelper
{
    public static Square[] rookDirections = new Square[] { new Square(1, 0), new Square(-1, 0), new Square(0, 1), new Square(0, -1) };
    public static Square[] bishopDirections = new Square[] { new Square(1, 1), new Square(-1, 1), new Square(1, -1), new Square(-1, -1) };
    public static int IndexToRank(int index) => index >> 3;
    public static int IndexToFile(int index) => index & 0b111;
    public static int FileRankToIndex(int file, int rank) => rank << 3 + file;
    public static string IndexToUCI(int index) => $"{(char)('a' + IndexToFile(index))}{IndexToRank(index) + 1}";
}
