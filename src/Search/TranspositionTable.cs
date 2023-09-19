using Chess;


namespace Search;

public class TranspositionTable
{
    int sizeInMb = 64;

    int numberOfEntriesPerTable;
    TranspostionEntry[] table;


    Board board;

    public TranspositionTable(Board board)
    {
        int sizeInBytes = sizeInMb * 1024 * 1024;
        int sizeOfEntry = TranspostionEntry.GetSize();
        int numberOfEntries = sizeInBytes / sizeOfEntry;
        numberOfEntriesPerTable = numberOfEntries / 1;

        this.board = board;
        table = new TranspostionEntry[numberOfEntriesPerTable];
    }


    public void Clear()
    {
        for (int i = 0; i < numberOfEntriesPerTable; i++)
        {
            table[i] = new();
        }
    }

    ulong index => board.gameState.zobristKey % (ulong)numberOfEntriesPerTable;

    public bool TryGetEvaluation(int alpha, int beta, int depth, bool pvNode, out int eval)
    {
        TranspostionEntry entry = table[index];

        if (entry.zobristKey == board.gameState.zobristKey && entry.depth >= depth && Math.Abs(entry.score) < (Searcher.MATE_SCORE - 100))
        {
            if (entry.flag == TranspostionEntry.EXACT)
            {
                eval = entry.score;
                return true;
            }

            if (entry.flag == TranspostionEntry.LOWERBOUND && entry.score >= beta) // && !pvNode
            {
                eval = entry.score;
                return true;
            }

            if (entry.flag == TranspostionEntry.UPPERBOUND && entry.score <= alpha) // && !pvNode
            {
                eval = entry.score;
                return true;
            }
        }
        eval = 0;
        return false;
    }

    public Move GetBestMove()
    {
        return table[index].bestMove;
    }

    public void Store(int depth, int eval, int flag, Move bestMove)
    {
        TranspostionEntry newEntry = new(board.gameState.zobristKey, (byte)depth, eval, bestMove, (byte)flag);
        table[index] = newEntry;
    }
}

