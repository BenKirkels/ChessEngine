using Chess;

namespace Search;

public struct TranspostionEntry
{
    public readonly ulong zobristKey;
    public readonly short depth;
    public readonly int score;
    public readonly Move bestMove;
    public readonly byte flag;

    public const int SIZE_OF_ENTRY = sizeof(ulong) + sizeof(short) + sizeof(int) + Move.SIZE_OF_MOVE + sizeof(byte);
    public static TranspostionEntry INVALID_ENTRY = new TranspostionEntry(0, 0, 0, Move.NullMove, 0);

    // Flags
    public const byte INVALID = 0;
    public const byte EXACT = 1;
    public const byte LOWERBOUND = 2;
    public const byte UPPERBOUND = 3;

    public TranspostionEntry(ulong zobristKey, short depth, int score, Move bestMove, byte flag)
    {
        this.zobristKey = zobristKey;
        this.depth = depth;
        this.score = score;
        this.bestMove = bestMove;
        this.flag = flag;
    }
}