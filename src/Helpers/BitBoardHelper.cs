using System.Numerics;

namespace Chess;
public static class BitBoardHelper
{
    public static int ClearAndGetIndexOfLSB(ref ulong bitboard)
    {
        int index = BitOperations.TrailingZeroCount(bitboard);
        ToggleBit(ref bitboard, index);
        return index;
    }

    /// <summary>
    /// Change the bit at the given index from 0 to 1 or from 1 to 0.
    /// </summary>
    /// <param name="bitboard"></param>
    /// <param name="index"></param>
    public static void ToggleBit(ref ulong bitboard, int index)
    {
        bitboard ^= 1UL << index;
    }

    /// <summary>
    /// Generate a bitboard with the given indexes set to 1.
    /// </summary>
    public static ulong Indexes(params int[] indexes)
    {
        ulong bitboard = 0;
        foreach (int index in indexes)
        {
            bitboard |= 1UL << index;
        }
        return bitboard;
    }

}
