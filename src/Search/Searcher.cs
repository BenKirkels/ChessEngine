using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Chess;
using Evaluation;

namespace Search;

public class Searcher
{
    const int MATE_SCORE = 100000;
    const int INFINITY = 9999999;

    const int MAX_DEPTH = 100;
    const int INNITIAL_WINDOW = 50;
    const int WINDOW_INCREMENT = 75;

    readonly TranspositionTable transpositionTable;
    readonly Stopwatch stopwatch;
    readonly Evaluator Evaluator;
    readonly MoveOrdener moveOrdener;

    Move BestMoveRoot;
    int searchTime;
    Board board;



    public void StartSearch(Board board, int searchTime)
    {
        stopwatch.Restart();

        BestMoveRoot = Move.NullMove;

        this.searchTime = searchTime;
        this.board = board;

        moveOrdener.ResetHistory();

        IterativeDeepening();
    }

    void IterativeDeepening()
    {
        int alpha = -INFINITY;
        int beta = INFINITY;

        int numberOfAspirationFails = 0;

        for (int depth = 2; depth <= MAX_DEPTH;)
        {
            // Search with aspiration window
            int eval = NegaMax(alpha, beta, depth, 0);

            // Check if we have run out of time
            if (TimeUp)
                break;

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

    int NegaMax(int alpha, int beta, int depth, int ply, bool pvNode = false)
    {
        int eval;
        if (depth <= 0)
        {
            eval = QuiescenceSearch(alpha, beta);
            if (TimeUp) return INFINITY;
            return eval;
        }

        bool isRoot = ply == 0;
        TranspostionEntry entry = transpositionTable.Get(board);



        // Check extension
        if (board.InCheck()) depth++;

        if (!isRoot)
        {
            if (board.IsRepetition())
            {
                return -ply;
            }

            // Transposition table
            if (false && entry.flag != TranspostionEntry.INVALID &&
                entry.depth >= depth &&
                Math.Abs(entry.score) < MATE_SCORE - 100) // Dont use mate scores
            {
                switch (entry.flag)
                {
                    case TranspostionEntry.EXACT:
                        return entry.score;
                    case TranspostionEntry.LOWERBOUND:
                        if (entry.score >= beta) return entry.score;
                        break;
                    case TranspostionEntry.UPPERBOUND:
                        if (entry.score <= alpha) return entry.score;
                        break;
                }
            }
            // TODO: Pruning

        } // End of !isRoot

        Span<Move> moves = board.GetLegalMoves();

        // Checkmate/Stalemate
        if (moves.Length == 0) return board.InCheck() ? -MATE_SCORE + ply : -ply;

        moveOrdener.OrdenMoves(ref moves, entry.bestMove);

        int movesSearched = 0;
        int origAlpha = alpha;
        int bestEval = -INFINITY;
        Move bestMove = Move.NullMove;
        foreach (Move move in moves)
        {
            if (TimeUp)
                return INFINITY;

            board.MakeMove(move);

            if (movesSearched++ == 0)
            {
                // Principal variation
                eval = -NegaMax(-beta, -alpha, depth - 1, ply + 1, true);
            }
            else
            {
                int depthReduction;
                // No pricipal variation
                if (movesSearched < 5 || depth < 3)
                    depthReduction = 0;
                else
                    // Late move reduction 
                    // using Sempa's formula: reduce by 1 ply for the first 6 moves, then by depth/3
                    depthReduction = movesSearched < 11 ? 1 : (depth / 3);

                eval = -NullWindowSearch(alpha, depth - 1 - depthReduction, ply + 1);
                if (eval > alpha)
                {
                    // Null window failed
                    eval = -NegaMax(-beta, -alpha, depth - 1 - depthReduction, ply + 1);
                }
            }
            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                if (bestEval > alpha)
                {
                    alpha = bestEval;
                    bestMove = move;
                    if (isRoot) BestMoveRoot = move;
                }

                if (alpha >= beta) // fail high
                {
                    if (!move.isCapture)
                    {
                        moveOrdener.StoreMove(move, depth);
                    }
                    break;
                }
            }

        }
        if (bestMove != Move.NullMove)
        {
            byte flag = bestEval >= beta ? TranspostionEntry.LOWERBOUND : bestEval > origAlpha ? TranspostionEntry.EXACT : TranspostionEntry.UPPERBOUND;
            TranspostionEntry newEntry = new(board.gameState.zobristKey, (short)depth, bestEval, bestMove, flag);
            transpositionTable.Store(newEntry);
        }
        return bestEval;
    }

    int NullWindowSearch(int alpha, int depth, int ply)
    {
        return NegaMax(-alpha - 1, -alpha, depth, ply);
    }

    int QuiescenceSearch(int alpha, int beta)
    {
        TranspostionEntry entry = transpositionTable.Get(board);

        // Transposition table
        if (false && entry.flag != TranspostionEntry.INVALID &&
            Math.Abs(entry.score) < MATE_SCORE - 100) // Dont use mate scores
        {
            switch (entry.flag)
            {
                case TranspostionEntry.EXACT:
                    return entry.score;
                case TranspostionEntry.LOWERBOUND:
                    if (entry.score >= beta) return entry.score;
                    break;
                case TranspostionEntry.UPPERBOUND:
                    if (entry.score <= alpha) return entry.score;
                    break;
            }
        }

        int staticEval = Evaluator.Evaluate(board);

        if (staticEval >= beta) return beta;
        if (staticEval > alpha) alpha = staticEval;

        Span<Move> moves = board.GetLegalMoves(false);
        moveOrdener.OrdenMoves(ref moves, entry.bestMove);

        foreach (Move move in moves)
        {
            if (TimeUp)
                return INFINITY;

            board.MakeMove(move);
            int eval = -QuiescenceSearch(-beta, -alpha);
            board.UndoMove(move);
            if (eval >= beta) return beta;
            if (eval > alpha) alpha = eval;
        }
        return alpha;
    }

    bool TimeUp => stopwatch.ElapsedMilliseconds > searchTime && BestMoveRoot != Move.NullMove;
    public Move GetBestMove() => BestMoveRoot;

    public void Reset()
    {
        BestMoveRoot = Move.NullMove;
        transpositionTable.Clear();
        moveOrdener.ResetHistory();
        moveOrdener.ResetKillerMoves();
    }
    public Searcher(Board board)
    {
        BestMoveRoot = Move.NullMove;
        transpositionTable = new TranspositionTable();
        stopwatch = new Stopwatch();
        Evaluator = new Evaluator();
        moveOrdener = new(board);
    }
}