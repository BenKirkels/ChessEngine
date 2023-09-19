using Chess;

namespace Search;

public struct TranspostionEntry
{
    public readonly ulong zobristKey;
    public readonly byte depth;
    public readonly int score;
    public readonly Move bestMove;
    public readonly byte flag;

    // Flags
    public const byte INVALID = 0;
    public const byte EXACT = 1;
    public const byte LOWERBOUND = 2;
    public const byte UPPERBOUND = 3;

    public TranspostionEntry(ulong zobristKey, byte depth, int score, Move bestMove, byte flag)
    {
        this.zobristKey = zobristKey;
        this.depth = depth;
        this.score = score;
        this.bestMove = bestMove;
        this.flag = flag;
    }

    public static int GetSize()
    {
        return System.Runtime.InteropServices.Marshal.SizeOf<TranspostionEntry>();
    }
}