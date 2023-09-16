using Chess;


namespace Search;

public class TranspositionTable
{
    private int sizeInMb = 64;
    private int sizeInBytes => sizeInMb * 1024 * 1024;
    private int sizeOfEntry = TranspostionEntry.SIZE_OF_ENTRY;
    private int numberOfEntries => sizeInBytes / sizeOfEntry;

    private TranspostionEntry[] table;

    public TranspositionTable()
    {
        table = new TranspostionEntry[numberOfEntries];
    }

    public TranspostionEntry Get(Board board)
    {
        ulong zobrist = board.gameState.zobristKey;

        TranspostionEntry entry = table[(int)(zobrist % (ulong)numberOfEntries)];

        if (entry.zobristKey == zobrist)
        {
            return entry;
        }
        return TranspostionEntry.INVALID_ENTRY;
    }

    public void Store(TranspostionEntry entry)
    {
        int index = (int)(entry.zobristKey % (ulong)numberOfEntries);
        table[index] = entry;
    }

    public void Clear()
    {
        table = new TranspostionEntry[numberOfEntries];
    }
}

