using System.Numerics;

namespace Chess;
public static class BitBoardHelper
{
    private const ulong rank1 = 0xFF;
    private const ulong fileA = 0x0101010101010101;
    public static ulong Rank(int rank) => rank1 << (rank * 8);
    public static ulong File(int file) => fileA << file;

    public static ulong pawnAttacks(int index, bool whitePiece)
    {
        ulong bitboard = 0;
        int rank = BoardHelper.IndexToRank(index);
        if (whitePiece)
        {
            if (rank != 0)
                bitboard |= Index(index + 7);
            if (rank != 7)
                bitboard |= Index(index + 9);
        }
        else
        {
            if (rank != 0)
                bitboard |= Index(index - 9);
            if (rank != 7)
                bitboard |= Index(index - 7);
        }
        return bitboard;
    }
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

    /// <summary>
    /// Generate a bitboard with the given index set to 1.
    /// </summary>
    public static ulong Index(int index)
    {
        return 1UL << index;
    }

    /// <summary>
    /// Generate a bitboard with all bits moves x ranks higher.
    /// </summary>
    public static ulong MoveXSquaresForward(ulong bitboard, bool whitePiece, int x)
    {
        return whitePiece ? bitboard << 8 * x : bitboard >> 8 * x;
    }
}
