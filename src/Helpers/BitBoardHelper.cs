using System.Numerics;

namespace Chess;
public static class BitBoardHelper
{
    public const ulong rank1 = 0xFF;
    public const ulong rank2 = 0xFF00;
    public const ulong rank3 = 0xFF0000;
    public const ulong rank4 = 0xFF000000;
    public const ulong rank5 = 0xFF00000000;
    public const ulong rank6 = 0xFF0000000000;
    public const ulong rank7 = 0xFF000000000000;
    public const ulong rank8 = 0xFF00000000000000;
    public const ulong fileA = 0x0101010101010101;
    public const ulong fileB = 0x0202020202020202;
    public const ulong fileC = 0x0404040404040404;
    public const ulong fileD = 0x0808080808080808;
    public const ulong fileE = 0x1010101010101010;
    public const ulong fileF = 0x2020202020202020;
    public const ulong fileG = 0x4040404040404040;
    public const ulong fileH = 0x8080808080808080;
    private const ulong diagonal = 0x8040201008040201;
    private const ulong antiDiagonal = 0x0102040810204080;
    public static ulong Rank(int rank) => rank1 << (rank * 8);
    public static ulong File(int file) => fileA << file;
    public static ulong Diagonal(int shiftRight) => diagonal << shiftRight;
    public static ulong AnitDiagonal(int shiftRight) => antiDiagonal << shiftRight;

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

    public static ulong untilEdge(int index, int dir)
    {
        ulong result = 0;
        for (int i = 1; i <= PrecomputedData.movesToEdge[index, dir]; i++)
            SetIndex(ref result, index + BoardHelper.directions[dir] * i);
        return result;
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
    public static ulong KingMoves(int index)
    {
        ulong result = 0;
        Square startSquare = new Square(index);

        Square[] moves = Square.rookDirections.Concat(Square.bishopDirections).ToArray();

        foreach (Square move in moves)
        {
            Square targetSquare = startSquare + move;
            if (targetSquare.IsValid)
                SetIndex(ref result, targetSquare.index);
        }
        return result;
    }

    public static ulong alignMask(int indexA, int indexB)
    {
        Square squareA = new(indexA);
        Square squareB = new(indexB);

        Square direction = squareA - squareB;

        if (direction.IsHorizontal) return Rank(squareA.rank);
        if (direction.IsVertical) return File(squareA.file);
        if (direction.IsDiagonal) return Diagonal(squareA.file - squareA.rank);
        if (direction.IsAntiDiagonal) return Diagonal(squareA.rank - squareA.file);

        return 0;
    }
}
