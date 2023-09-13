namespace Chess;
public static class BoardHelper
{
    public static int[] directions = new int[] { 8, 1, -8, -1, 9, -7, -9, 7 };
    public static int IndexToRank(int index) => index >> 3;
    public static int IndexToFile(int index) => index & 0b111;
    public static int FileRankToIndex(int file, int rank) => (rank << 3) + file;
    public static string IndexToUCI(int index) => $"{(char)('a' + IndexToFile(index))}{IndexToRank(index) + 1}";
}
