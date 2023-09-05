using System.ComponentModel;
using System.Reflection.Metadata;

namespace Chess;

public class MoveGen
{
    int numberOfMoves = 0;
    Board board;
    bool generateQuietMoves;
    bool whiteToMove;
    int myColor;
    int opponentColor;
    ulong myPiecesBitboard;
    ulong opponentPiecesBitboard;
    ulong allPiecesBitboard;



    public void GenerateMoves(ref Span<Move> moves, Board b, bool ignoreQuietMoves)
    {
        numberOfMoves = 0;
        board = b;
        generateQuietMoves = ignoreQuietMoves;

        Init();

        PawnMoves(ref moves);
    }

    private void Init()
    {
        whiteToMove = board.whiteToMove;
        myColor = board.myColor;
        opponentColor = board.opponentColor;

        myPiecesBitboard = board.colorBitboards[myColor];
        opponentPiecesBitboard = board.colorBitboards[opponentColor];
        allPiecesBitboard = board.allPiecesBitboard;
    }

    public void PawnMoves(ref Span<Move> moves)
    {
        ulong pawns = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, myColor)];
        while (pawns != 0)
        {
            int pawn = BitBoardHelper.ClearAndGetIndexOfLSB(ref pawns);

        }
    }
}