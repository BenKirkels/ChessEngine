using System.ComponentModel;
using System.Numerics;
using System.Xml;
using Chess;
using Helpers;

namespace Evaluation;

public class Evaluator
{
    //------------- EVALUATION CONSTANTS -------------
    static readonly int[] pieceValues = new int[] { 0, 60, 310, 310, 435, 910, 0, 0, 90, 310, 310, 550, 1000, 0 };
    static readonly int[] rookAdjustment = { 15, 12, 9, 6, 3, 0, -3, -6, -9 }; // number of pawns as index
    static readonly int[] knightAdjustment = { -20, -16, -12, -8, -4, 0, 4, 8, 12 }; // number of pawns as index
    //const int badBishopPenalty = 10;
    const int bishopPair = 20;
    const int knightPair = 10;
    const int rookPair = 10;
    const int undevelopedPiecePenalty = 10;
    const int doubledPawnPenalty = 10;
    const int isolatedPawnPenalty = 20;
    const int passedPawnBonus = 20;
    const int tempoBonus = 0; // 10
    //--------------------------------------------------------
    const int WHITE = Board.WHITE_INDEX;
    const int BLACK = Board.BLACK_INDEX;
    readonly Board board;


    int gamePhase;
    int eval;
    int middleGame;
    int endGame;



    public Evaluator(Board board)
    {
        this.board = board;
    }
    void Init()
    {
        gamePhase = 0;
        eval = 0;
        middleGame = 0;
        endGame = 0;
    }

    public int Evaluate()
    {
        // TODO: add evaluation hash table


        Init();


        // Evaluate material
        for (int piece = Piece.WHITE_PAWN; piece <= Piece.WHITE_KING; piece++)
        {
            middleGame += board.pieceCount[piece] * pieceValues[Piece.PieceType(piece)];
            endGame += board.pieceCount[piece] * pieceValues[Piece.PieceType(piece) + 7];
        }
        for (int piece = Piece.BLACK_PAWN; piece <= Piece.BLACK_KING; piece++)
        {
            middleGame -= board.pieceCount[piece] * pieceValues[Piece.PieceType(piece)];
            endGame -= board.pieceCount[piece] * pieceValues[Piece.PieceType(piece) + 7];
        }

        // Evaluate piece square tables
        middleGame += board.pcsqMg[WHITE] - board.pcsqMg[BLACK];
        endGame += board.pcsqEg[WHITE] - board.pcsqEg[BLACK];

        // Evaluate piece pair bonuses
        if (board.pieceCount[Piece.WHITE_BISHOP] >= 2)
            eval += bishopPair;
        if (board.pieceCount[Piece.BLACK_BISHOP] >= 2)
            eval -= bishopPair;
        if (board.pieceCount[Piece.WHITE_KNIGHT] >= 2)
            eval -= knightPair;
        if (board.pieceCount[Piece.BLACK_KNIGHT] >= 2)
            eval += knightPair;
        if (board.pieceCount[Piece.WHITE_ROOK] >= 2)
            eval -= rookPair;
        if (board.pieceCount[Piece.BLACK_ROOK] >= 2)
            eval += rookPair;


        for (int index = 0; index < 64; index++)
        {
            int piece = board.squares[index];
            if (piece == Piece.NONE)
                continue;

            int pieceType = Piece.PieceType(piece);
            int color = Piece.Color(piece);

            switch (pieceType)
            {
                case Piece.PAWN:
                    EvalPawn(index, color);
                    break;
                case Piece.KNIGHT:
                    EvalKnight(index, color);
                    break;
                case Piece.BISHOP:
                    EvalBishop(index, color);
                    break;
                case Piece.ROOK:
                    EvalRook(index, color);
                    break;
                case Piece.QUEEN:
                    EvalQueen(index, color);
                    break;
            }
        }
        eval += (middleGame * gamePhase + endGame * (24 - gamePhase)) / 24;
        return (board.whiteToMove ? eval : -eval) + tempoBonus;
    }
    void EvalPawn(int index, int color)
    {
        int colorIndex = color == Piece.WHITE ? Board.WHITE_INDEX : Board.BLACK_INDEX;
        int sign = color == Piece.WHITE ? 1 : -1;
        ulong myPawns = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, color)];
        ulong enemyPawns = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, color ^ Piece.BLACK)];

        // Doubled pawn penalty
        if ((myPawns & PawnMasks.doubledPawn[colorIndex, index]) != 0)
            eval -= sign * doubledPawnPenalty;

        // Isolated pawn penalty
        if ((myPawns & PawnMasks.isolatedPawn[colorIndex, index]) == 0)
            eval -= sign * isolatedPawnPenalty;

        // Passed pawn bonus
        if ((enemyPawns & PawnMasks.passedPawn[colorIndex, index]) == 0)
            eval += sign * passedPawnBonus;

    }
    void EvalKnight(int index, int color)
    {
        gamePhase += 1;
        if (color == Piece.WHITE)
            eval += knightAdjustment[board.pieceCount[Piece.MakePiece(Piece.PAWN, color)]]; // adjust knight value based on number of pawns
        else
            eval -= knightAdjustment[board.pieceCount[Piece.MakePiece(Piece.PAWN, color)]]; // adjust knight value based on number of pawns

        // Undeveloped piece penalty
        if (board.CanCastle(color)) // Allow piece to return after castling
        {
            if (color == Piece.WHITE)
            {
                if (index == 1 || index == 6)
                    eval -= undevelopedPiecePenalty;
            }
            else
            {
                if (index == 57 || index == 62)
                    eval += undevelopedPiecePenalty;
            }
        }
    }
    void EvalBishop(int index, int color)
    {
        gamePhase += 1;

        // Undeveloped piece penalty
        if (board.CanCastle(color)) // Allow piece to return after castling
        {
            if (color == Piece.WHITE)
            {
                if (index == 2 || index == 5)
                    eval -= undevelopedPiecePenalty;
            }
            else
            {
                if (index == 58 || index == 61)
                    eval += undevelopedPiecePenalty;
            }
        }
    }
    void EvalRook(int index, int color)
    {
        gamePhase += 2;
        if (color == Piece.WHITE)
            eval += rookAdjustment[board.pieceCount[Piece.MakePiece(Piece.PAWN, color)]]; // adjust rook value based on number of pawns
        else
            eval -= rookAdjustment[board.pieceCount[Piece.MakePiece(Piece.PAWN, color)]]; // adjust rook value based on number of pawns
    }
    void EvalQueen(int index, int color)
    {
        gamePhase += 4;
    }
}
