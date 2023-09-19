using Chess;
using MoveGeneration;
using Search;
namespace ChessEngine;

public class EngineUCI
{
    readonly Board board;
    readonly Searcher searcher;


    static readonly string[] positionLabels = new string[] { "startpos", "fen", "moves" };
    static readonly string[] goLabels = new string[] { "go", "movetime", "wtime", "btime", "winc", "binc", "movestogo" };
    public EngineUCI()
    {
        board = new Board();
        searcher = new Searcher(board);
    }

    public void ReceivedMessage(string message)
    {
        message = message.Trim();

        string messageType = message.Split(' ')[0].ToLower();

        switch (messageType)
        {
            case "uci":
                Respond("uciok");
                break;
            case "isready":
                Respond("readyok");
                break;
            case "ucinewgame":
                NewGame();
                break;
            case "position":
                HandlePositionCommand(message);
                break;
            case "go":
                HandleGoCommand(message);
                break;
        }
    }
    void Respond(string message)
    {
        Console.WriteLine(message);
    }

    void NewGame()
    {
        // Call static classes to calculate precomputed data
        PrecomputedData.RookMoves(0, 0);
        Zobrist.GenerateZobristKey(new ulong[Piece.MAX_PIECE_NUMBER], 0, 0, true);
    }

    // Format: 'position startpos moves e2e4 e7e5'
    // Or: 'position fen rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 moves e2e4 e7e5'
    // Note: 'moves' section is optional
    void HandlePositionCommand(string message)
    {
        // FEN
        if (message.ToLower().Contains("startpos"))
        {
            board.SetPosition();
        }
        else if (message.ToLower().Contains("fen"))
        {
            string fen = TryGetLabelledValue(message, "fen", positionLabels);
            board.SetPosition(fen);
        }
        else
        {
            Console.WriteLine("Error: Invalid position command (expected 'startpos' or 'fen'))");
        }

        // Moves
        string moves = TryGetLabelledValue(message, "moves", positionLabels);
        if (!string.IsNullOrEmpty(moves))
        {
            string[] moveList = moves.Split(' ');
            foreach (string move in moveList)
            {
                board.MakeMove(new Move(move, board));
            }
        }

    }

    void HandleGoCommand(string message)
    {
        int searchTime;
        if (message.ToLower().Contains("movetime"))
        {
            searchTime = TryGetLabelledValueInt(message, "movetime", goLabels);
        }
        else
        {
            int wtime = TryGetLabelledValueInt(message, "wtime", goLabels);
            int btime = TryGetLabelledValueInt(message, "btime", goLabels);
            int winc = TryGetLabelledValueInt(message, "winc", goLabels);
            int binc = TryGetLabelledValueInt(message, "binc", goLabels);

            searchTime = TimeCalculation(wtime, btime, winc, binc);
        }
        searcher.StartSearch(searchTime);
        string bestMove = searcher.GetBestMove().ToString();
        Respond($"bestmove {bestMove}");
    }
    int TimeCalculation(int whiteTimeLeft, int blackTimeLeft, int whiteIncrement, int blackIncrement)
    {
        bool IAmWhite = board.whiteToMove;
        int myTimeLeft = IAmWhite ? whiteTimeLeft : blackTimeLeft;
        int myIncrement = IAmWhite ? whiteIncrement : blackIncrement;

        int timeToSpend = myTimeLeft / 30 + myIncrement / 2;

        int minThinkTime = (int)Math.Min(50, myTimeLeft / 4);
        return Math.Max(minThinkTime, timeToSpend);
    }

    int TryGetLabelledValueInt(string message, string label, string[] allLabels, int defaultValue = 0)
    {
        string value = TryGetLabelledValue(message, label, allLabels);
        if (int.TryParse(value.Split(" ")[0], out int result))
        {
            return result;
        }
        return defaultValue;
    }
    string TryGetLabelledValue(string message, string label, string[] allLabels, string defaultValue = "")
    {
        message = message.Trim().ToLower();
        if (message.Contains(label))
        {
            int valueStart = message.IndexOf(label) + label.Length;
            int valueEnd = message.Length;
            foreach (string otherLabel in allLabels)
            {
                if (otherLabel != label && message.Contains(otherLabel))
                {
                    int otherLabelStart = message.IndexOf(otherLabel);
                    if (otherLabelStart < valueEnd && otherLabelStart > valueStart)
                    {
                        valueEnd = otherLabelStart;
                    }
                }
            }
            return message.Substring(valueStart, valueEnd - valueStart).Trim();
        }
        return defaultValue;
    }

}
