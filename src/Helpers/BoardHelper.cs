namespace Chess;
public static class BoardHelper
{
    public static int IndexToRank(int index) => index / 8;
    public static int IndexToFile(int index) => index % 8;

    public static string IndexToUCI(int index) => $"{(char)('a' + IndexToFile(index))}{IndexToRank(index) + 1}";
}
