using Chess;


namespace Search;

public class TranspositionTable
{
    private int sizeInMb = 64;
    private int sizeInBytes => sizeInMb * 1024 * 1024;
    private int sizeOfEntry = TranspostionEntry.SIZE_OF_ENTRY;
    private int numberOfEntries => sizeInBytes / sizeOfEntry;
    private int numberOfEntriesPerTable => numberOfEntries / 2;

    private TranspostionEntry[] tableDepthPrefered;
    private TranspostionEntry[] tableAlwaysReplace;

    public TranspositionTable()
    {
        tableDepthPrefered = new TranspostionEntry[numberOfEntriesPerTable];
        tableAlwaysReplace = new TranspostionEntry[numberOfEntriesPerTable];
    }

    public TranspostionEntry Get(Board board)
    {
        ulong zobrist = board.gameState.zobristKey;
        int index = (int)(zobrist % (ulong)numberOfEntriesPerTable);

        TranspostionEntry entry = tableDepthPrefered[index];

        if (entry.zobristKey == zobrist)
        {

            return entry;
        }

        entry = tableAlwaysReplace[index];

        if (entry.zobristKey == zobrist)
        {
            return entry;
        }

        return TranspostionEntry.INVALID_ENTRY;
    }

    public void Store(TranspostionEntry entry)
    {
        int index = (int)(entry.zobristKey % (ulong)numberOfEntriesPerTable);

        if (entry.depth > tableDepthPrefered[index].depth)
        {
            tableDepthPrefered[index] = entry;
        }
        else if (entry.zobristKey != tableDepthPrefered[index].zobristKey) // If the position is already in the depth prefered table, we don't want to replace it in the always replace table
        {
            tableAlwaysReplace[index] = entry;
        }
    }

    public void Clear()
    {
        tableDepthPrefered = new TranspostionEntry[numberOfEntriesPerTable];
        tableAlwaysReplace = new TranspostionEntry[numberOfEntriesPerTable];
    }
}

