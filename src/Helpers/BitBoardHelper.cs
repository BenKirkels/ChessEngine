using System.Numerics;

namespace Chess;
public static class BitBoardHelper
{
    private const ulong rank1 = 0xFF;
    private const ulong fileA = 0x0101010101010101;
    public static ulong Rank(int rank) => rank1 << (rank * 8);
    public static ulong File(int file) => fileA << file;

    public static int ClearAndGetIndexOfLSB(ref ulong bitboard)
    {
        int index = BitOperations.TrailingZeroCount(bitboard);
        ToggleIndex(ref bitboard, index);
        return index;
    }

    /// <summary>
    /// Change the bit at the given index from 0 to 1 or from 1 to 0.
    /// </summary>
    /// <param name="bitboard"></param>
    /// <param name="index"></param>
    public static void ToggleIndex(ref ulong bitboard, int index)
    {
        bitboard ^= 1UL << index;
    }

    public static void SetIndex(ref ulong bitboard, int index)
    {
        bitboard |= 1UL << index;
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
    public static ulong Index(int rank, int file)
    {
        return 1UL << (rank * 8 + file);
    }

    public static bool IsBitSet(ulong bitboard, int index)
    {
        return (bitboard & (1UL << index)) != 0;
    }

    public static ulong pawnAttacks(int index, bool whitePiece)
    {
        ulong bitboard = 0;
        int file = BoardHelper.IndexToFile(index);
        if (whitePiece)
        {
            if (file != 0)
                bitboard |= Index(index + 7);
            if (file != 7)
                bitboard |= Index(index + 9);
        }
        else
        {
            if (file != 0)
                bitboard |= Index(index - 9);
            if (file != 7)
                bitboard |= Index(index - 7);
        }
        return bitboard;
    }

    public static ulong KnightMoves(int index)
    {
        ulong bitboard = 0;
        Square startSquare = new Square(index);

        Square[] moves = new Square[] {
            new Square(1, 2),
            new Square(2, 1),
            new Square(2, -1),
            new Square(1, -2),
            new Square(-1, -2),
            new Square(-2, -1),
            new Square(-2, 1),
            new Square(-1, 2)
        };

        foreach (Square move in moves)
        {
            Square targetSquare = startSquare + move;
            if (targetSquare.IsValid)
                SetIndex(ref bitboard, targetSquare.index);
        }
        return bitboard;
    }
}
