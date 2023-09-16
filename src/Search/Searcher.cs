using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Chess;

namespace Search;

public class Searcher
{
    const int BEST_MATE = 100000;
    const int WORST_MATE = -100000;

    const int MAX_DEPTH = 100;
    const int INNITIAL_WINDOW = 50;
    const int WINDOW_INCREMENT = 75;

    readonly TranspositionTable transpositionTable = new TranspositionTable();
    readonly Stopwatch stopwatch = new Stopwatch();

    Move BestMoveRoot;
    int searchTime;
    Board board;



    public void StartSearch(Board board, int searchTime)
    {
        stopwatch.Restart();

        this.searchTime = searchTime;
        this.board = board;

        IterativeDeepening();
    }

    void IterativeDeepening()
    {
        int alpha = WORST_MATE;
        int beta = BEST_MATE;

        int numberOfAspirationFails = 0;

        for (int depth = 2; depth <= MAX_DEPTH;)
        {
            // Search with aspiration window
            int eval = NegaMax(alpha, beta, depth, 0);

            // Check if we have run out of time
            if (stopwatch.ElapsedMilliseconds > searchTime)
            {
                break;
            }

            // Check if we are in aspiration window and increase depth if we are
            // Otherwise widen the window and try again
            if (InAspirationWindow(eval, ref alpha, ref beta, ref numberOfAspirationFails))
            {
                depth++;
            }
        }
    }

    bool InAspirationWindow(int eval, ref int alpha, ref int beta, ref int numberOfFails)
    {
        if (alpha < eval && eval < beta)
        {
            // Set window for next iteration
            alpha = eval - INNITIAL_WINDOW / 2;
            beta = eval + INNITIAL_WINDOW / 2;
            numberOfFails = 0;
            return true;
        }
        if (eval <= alpha)
        {
            // Widen search window
            alpha -= WINDOW_INCREMENT * ++numberOfFails;
        }
        else
        {
            // Widen search window
            beta += WINDOW_INCREMENT * ++numberOfFails;
        }
        return false;
    }

    int NegaMax(int alpha, int beta, int depth, int ply)
    {
        bool isRoot = ply == 0;
        bool isQuiescence = depth <= 0;

        throw new System.NotImplementedException();
    }

    public void Reset()
    {
        transpositionTable.Clear();
    }
}