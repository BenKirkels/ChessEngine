using Chess;
using Helpers;
using Search;

namespace ChessEngine;

public class Bot
{
    private bool IAmWhite;

    readonly Board board;
    readonly Searcher searcher;

    public Bot()
    {
        board = new Board();
        searcher = new Searcher();
    }

    public void StartSearch(int searchTime)
    {
        searcher.StartSearch(board, searchTime);
    }

    public void PlayMove(string m)
    {
        board.MakeMove(new Move(m, board));
    }

    public void NewGame()
    {
        board.Reset();
        searcher.Reset();
    }

    public void SetPosition()
    {
        NewGame();

        board.SetPosition();
        IAmWhite = true;
    }
    public void SetPosition(string fen)
    {
        NewGame();

        board.SetPosition(fen);
        IAmWhite = board.whiteToMove;
    }

    public int TimeCalculation(int whiteTimeLeft, int blackTimeLeft, int whiteIncrement, int blackIncrement)
    {
        int myTimeLeft = IAmWhite ? whiteTimeLeft : blackTimeLeft;
        int myIncrement = IAmWhite ? whiteIncrement : blackIncrement;

        int timeToSpend = myTimeLeft / 30 + myIncrement / 2;

        int minThinkTime = (int)Math.Min(50, myTimeLeft / 4);
        return Math.Max(minThinkTime, timeToSpend);
    }
}