namespace chess
{
    public static class BoardHelper
    {
        public static int IndexToRank(int index) => index / 8;
        public static int IndexToFile(int index) => index % 8;
    }
}