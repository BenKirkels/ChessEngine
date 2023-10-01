using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Chess;
using Evaluation;

namespace Search;

public class Searcher
{
    public const int MATE_SCORE = 100000;
    public const int INFINITY = 9999999;

    const int MAX_DEPTH = 100;
    const int INNITIAL_WINDOW = 50;
    const int WINDOW_INCREMENT = 75;

    readonly TranspositionTable transpositionTable;
    readonly Stopwatch stopwatch;
    readonly Evaluator Evaluator;
    readonly MoveOrdener moveOrdener;
    SearchInfo searchInfo = new();

    Move BestMoveRoot;
    int searchTime;
    Board board;



    public void StartSearch(int searchTime)
    {
        stopwatch.Restart();

        searchInfo = new();

        BestMoveRoot = Move.NullMove;

        this.searchTime = searchTime;

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
            {
                searchInfo.depth = depth;
                PrintSearchInfo();
                break;
            }

            // Check if we are in aspiration window and increase depth if we are
            // Otherwise widen the window and try again
            if (InAspirationWindow(eval, ref alpha, ref beta, ref numberOfAspirationFails))
            {
                searchInfo.depth = depth;
                PrintSearchInfo();
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
        searchInfo.nodes++;
        int eval;
        if (depth <= 0)
        {
            eval = QuiescenceSearch(alpha, beta);
            if (TimeUp) return INFINITY;
            return eval;
        }

        bool isRoot = ply == 0;

        // Check extension
        if (board.InCheck()) depth++;

        if (!isRoot)
        {
            if (board.IsRepetition())
            {
                return -ply;
            }

            // Transposition table
            if (transpositionTable.TryGetEvaluation(alpha, beta, depth, pvNode, out eval))
            {
                searchInfo.tbhits++;
                return eval;
            }
            // TODO: Pruning

        } // End of !isRoot

        Span<Move> moves = board.GetLegalMoves();

        // Checkmate/Stalemate
        if (moves.Length == 0) return board.InCheck() ? -MATE_SCORE + ply : -ply;

        moveOrdener.OrdenMoves(ref moves, transpositionTable.GetBestMove());

        int movesSearched = 0;
        int origAlpha = alpha;
        int bestEval = -INFINITY;
        Move bestMove = Move.NullMove;
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (movesSearched++ == 0)
            {
                // Principal variation
                eval = -NegaMax(-beta, -alpha, depth - 1, ply + 1, true);
            }
            else
            {
                eval = -NegaMax(-alpha - 1, -alpha, depth - 1, ply + 1);
                if (eval > alpha && eval < beta)
                {
                    // Null window failed
                    eval = -NegaMax(-beta, -alpha, depth - 1, ply + 1);
                }
            }
            board.UndoMove(move);

            if (TimeUp)
                return INFINITY;

            if (eval > bestEval)
            {
                bestEval = eval;
                if (bestEval > alpha)
                {
                    alpha = bestEval;
                    bestMove = move;
                    if (isRoot)
                    {
                        BestMoveRoot = move;
                        searchInfo.score = bestEval;
                    }
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

        byte flag = bestEval >= beta ? TranspostionEntry.LOWERBOUND : bestEval > origAlpha ? TranspostionEntry.EXACT : TranspostionEntry.UPPERBOUND;
        transpositionTable.Store(depth, bestEval, flag, bestMove);

        return bestEval;
    }

    int QuiescenceSearch(int alpha, int beta)
    {
        searchInfo.nodes++;

        int staticEval = Evaluator.Evaluate();

        if (staticEval >= beta) return beta;
        if (staticEval > alpha) alpha = staticEval;

        Span<Move> moves = board.GetLegalMoves(false);
        moveOrdener.OrdenMoves(ref moves, Move.NullMove);

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
        this.board = board;
        BestMoveRoot = Move.NullMove;
        transpositionTable = new TranspositionTable(board);
        stopwatch = new Stopwatch();
        Evaluator = new Evaluator(board);
        moveOrdener = new(board);
    }

    void PrintSearchInfo()
    {
        Console.WriteLine($"info depth {searchInfo.depth} time {stopwatch.ElapsedMilliseconds} nodes {searchInfo.nodes} score cp {searchInfo.score} nps {1000 * searchInfo.nodes / (stopwatch.ElapsedMilliseconds + 1)} tbhits {searchInfo.tbhits} pv {BestMoveRoot}");
    }
    public struct SearchInfo
    {
        public int depth;
        public int nodes;
        public int score;
        public int tbhits;
        public SearchInfo()
        {
            depth = 0;
            nodes = 0;
            score = 0;
            tbhits = 0;
        }
    }
}