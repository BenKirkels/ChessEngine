using Chess;

namespace Search;

public struct TranspostionEntry
{
    public readonly ulong zobristKey;
    public readonly short depth;
    public readonly int score;
    public readonly Move bestMove;
    public readonly byte type;

    public const int SIZE_OF_ENTRY = sizeof(ulong) + sizeof(short) + sizeof(int) + Move.SIZE_OF_MOVE + sizeof(byte);
    public static TranspostionEntry INVALID_ENTRY = new TranspostionEntry(0, 0, 0, new Move(0, 0, 0), 0);

    public enum EntryType
    {
        INVALID,
        EXACT,
        LOWERBOUND,
        UPPERBOUND
    }

    public TranspostionEntry(ulong zobristKey, short depth, int score, Move bestMove, byte type)
    {
        this.zobristKey = zobristKey;
        this.depth = depth;
        this.score = score;
        this.bestMove = bestMove;
        this.type = type;
    }
}