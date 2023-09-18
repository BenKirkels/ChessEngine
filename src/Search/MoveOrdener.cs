using Chess;

namespace Search;

public class MoveOrdener
{
    const int TRANSPOSITION_SCORE = 10_000_000;
    const int CAPTURE = 1_000_000;
    const int FIRST_KILLERMOVE = 900_001;
    const int SECOND_KILLERMOVE = 900_000;
    int[] moveScores;
    Board board;

    Move[,] killerMoves;
    int[,,] history;

    public MoveOrdener(Board board)
    {
        this.board = board;
        moveScores = new int[Board.MAX_MOVES];
        killerMoves = new Move[2, 64];
        history = new int[2, 7, 64];
    }
    public void ResetKillerMoves() => killerMoves = new Move[2, 64];
    public void ResetHistory() => history = new int[2, 7, 64];

    public void StoreMove(Move move, int depth)
    {
        // Store killer moves
        if (!(killerMoves[0, board.plyCount % 64] == move))
        {
            killerMoves[1, board.plyCount % 64] = killerMoves[0, board.plyCount % 64];
            killerMoves[0, board.plyCount % 64] = move;
        }
        // Update history
        history[board.plyCount & 1, board.MovePieceType(move), move.to] += depth * depth;

    }
    public void OrdenMoves(ref Span<Move> moves, Move ttMove)
    {
        int movesOrdened = 0;
        foreach (Move move in moves)
        {
            // Negative sign because .Sort() sorst ascending
            moveScores[movesOrdened++] = -(
                move == ttMove ? TRANSPOSITION_SCORE :
                move.isCapture ? CAPTURE * board.CapturePieceType(move) - board.MovePieceType(move) :
                move == killerMoves[0, board.plyCount % 64] ? FIRST_KILLERMOVE :
                move == killerMoves[1, board.plyCount % 64] ? SECOND_KILLERMOVE :
                history[board.plyCount & 1, board.MovePieceType(move), board.CapturePieceType(move)]
            );
        }
        moveScores.AsSpan(0, moves.Length).Sort(moves);
    }
}