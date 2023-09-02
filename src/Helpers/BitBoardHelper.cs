using System.Numerics;

namespace chess
{
    public static class BitBoardHelper
    {
        public static int ClearAndGetIndexOfLSB(ref ulong bitboard)
        {
            int index = BitOperations.TrailingZeroCount(bitboard);
            ToggleBit(ref bitboard, index);
            return index;
        }

        public static void ToggleBit(ref ulong bitboard, int index)
        {
            bitboard ^= 1UL << index;
        }
    }
}