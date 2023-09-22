#undef DEBUG

using System.Diagnostics;
using Chess;

namespace ChessEngine;
public static class Program
{
    public static void Main(string[] args)
    {
#if DEBUG
        Stopwatch stopwatch = new();
        Stopwatch stopwatchTotal = new();
        Console.WriteLine("start tests");  // Nothing | Zobrist

        string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        Board board = Board.CreateBoardFromFen(fen);
        stopwatchTotal.Start();

        stopwatch.Start();
        //_ = CountMoves(board, 7);
        stopwatch.Stop();
        Console.WriteLine($"Test1: {stopwatch.ElapsedMilliseconds}ms"); // 583_072ms | 286_488ms

        fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        board = Board.CreateBoardFromFen(fen);

        stopwatch.Restart();
        //_ = CountMoves(board, 5);
        stopwatch.Stop();

        Console.WriteLine($"Test2: {stopwatch.ElapsedMilliseconds}ms"); // (35_411ms)(-1) | (18222ms) |

        fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        board = Board.CreateBoardFromFen(fen);

        stopwatch.Restart();
        _ = CountMoves(board, 8);
        stopwatch.Stop();

        Console.WriteLine($"Test3: {stopwatch.ElapsedMilliseconds}ms"); // 481_443ms | 343_202ms

        stopwatchTotal.Stop();

        Console.WriteLine($"Total: {stopwatchTotal.ElapsedMilliseconds}ms");
#endif
        EngineUCI engine = new EngineUCI();
        string message = string.Empty;

        while (message != "quit")
        {
            message = Console.ReadLine();
            engine.ReceivedMessage(message);
        }
    }
#if DEBUG
    static int CountMoves(Board board, int depth)
    {
        if (depth == 0)
            return 1;
        int count = 0;

        Span<Move> moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int moveCount = CountMoves(board, depth - 1);
            count += moveCount;
            board.UndoMove(move);
        }
        return count;
    }
#endif
}
