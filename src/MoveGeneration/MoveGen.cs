using System.ComponentModel;
using System.Reflection.Metadata;

namespace Chess;

public class MoveGen
{
    int numberOfMoves;
    Board board;
    bool generateQuietMoves;
    bool whiteToMove;
    int myColor;
    int myColorIndex;
    int opponentColor;
    int opponentColorIndex;
    ulong myPiecesBitboard;
    ulong opponentPiecesBitboard;
    ulong allPiecesBitboard;
    int enPassantSquare;



    public void GenerateMoves(ref Span<Move> moves, Board board, bool generateQuietMoves)
    {
        this.board = board;
        this.generateQuietMoves = generateQuietMoves;

        Init();

        PawnMoves(ref moves);
        moves = moves[..numberOfMoves];
    }

    void Init()
    {
        numberOfMoves = 0;

        whiteToMove = board.whiteToMove;
        myColor = board.myColor;
        myColorIndex = board.myColorIndex;
        opponentColor = board.opponentColor;
        opponentColorIndex = board.opponentColorIndex;

        myPiecesBitboard = board.colorBitboards[myColorIndex];
        opponentPiecesBitboard = board.colorBitboards[opponentColorIndex];
        allPiecesBitboard = board.allPiecesBitboard;

        enPassantSquare = board.gameState.enPassantSquare;
    }

    public void PawnMoves(ref Span<Move> moves)
    {
        int direction = whiteToMove ? 1 : -1;
        int pushOffset = direction * 8;

        ulong pawnsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, myColor)];
        while (pawnsBitboard != 0)
        {
            int pawnIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref pawnsBitboard);
            int rank = BoardHelper.IndexToRank(pawnIndex);
            bool canPromote = rank == (whiteToMove ? 6 : 1);
            bool canDoublePush = rank == (whiteToMove ? 1 : 6);

            // Pawn pushes
            if (generateQuietMoves || canPromote)
                if ((allPiecesBitboard & BitBoardHelper.Index(pawnIndex + pushOffset)) == 0) // Check if the square in front of the pawn is empty
                {
                    if (canPromote)
                    {
                        // Promotions
                        moves[numberOfMoves++] = new Move(pawnIndex, pawnIndex + pushOffset, Move.QUEEN_PROMOTION);
                        moves[numberOfMoves++] = new Move(pawnIndex, pawnIndex + pushOffset, Move.KNIGHT_PROMOTION);
                        moves[numberOfMoves++] = new Move(pawnIndex, pawnIndex + pushOffset, Move.ROOK_PROMOTION);
                        moves[numberOfMoves++] = new Move(pawnIndex, pawnIndex + pushOffset, Move.BISHOP_PROMOTION);
                    }
                    else
                        // Single push
                        moves[numberOfMoves++] = new Move(pawnIndex, pawnIndex + pushOffset, Move.QUIET_MOVE);

                    if (canDoublePush)
                    {
                        // Double push
                        if ((allPiecesBitboard & BitBoardHelper.Index(pawnIndex + pushOffset * 2)) == 0) // Check if the square two squares in front of the pawn is also empty
                            moves[numberOfMoves++] = new Move(pawnIndex, pawnIndex + pushOffset * 2, Move.DOUBLE_PAWN_PUSH);
                    }
                }

            // Pawn captures
            ulong capturableSquares = BitBoardHelper.pawnAttacks(pawnIndex, whiteToMove) & (opponentPiecesBitboard | BitBoardHelper.Index(enPassantSquare));
            while (capturableSquares != 0)
            {
                int captureIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref capturableSquares);
                if (canPromote)
                {
                    // Promotions
                    moves[numberOfMoves++] = new Move(pawnIndex, captureIndex, Move.QUEEN_PROMOTION_CAPTURE);
                    moves[numberOfMoves++] = new Move(pawnIndex, captureIndex, Move.KNIGHT_PROMOTION_CAPTURE);
                    moves[numberOfMoves++] = new Move(pawnIndex, captureIndex, Move.ROOK_PROMOTION_CAPTURE);
                    moves[numberOfMoves++] = new Move(pawnIndex, captureIndex, Move.BISHOP_PROMOTION_CAPTURE);
                }
                else if (captureIndex == enPassantSquare)
                    // En passant
                    moves[numberOfMoves++] = new Move(pawnIndex, captureIndex, Move.EN_PASSANT);
                else
                    // Normal capture
                    moves[numberOfMoves++] = new Move(pawnIndex, captureIndex, Move.CAPTURE);
            }
        }
    }
}