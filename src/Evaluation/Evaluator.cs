using System.Numerics;
using Chess;
using Helpers;

namespace Evaluation;

public class Evaluator
{
    int[] PieceValues = new int[] { 0, 60, 310, 320, 435, 910, 0, 0, 90, 310, 320, 550, 970 };
    int[] PiecePhase = new int[] { 0, 0, 1, 1, 2, 4, 0 };

    public int Evaluate(Board board)
    {
        int eval = 0;
        int phase = 0;
        int earlyGameEval = 0;
        int lateGameEval = 0;

        ulong whitePawns = board.pieceBitboards[Piece.WHITE_PAWN];
        ulong whiteKnights = board.pieceBitboards[Piece.WHITE_KNIGHT];
        ulong whiteBishops = board.pieceBitboards[Piece.WHITE_BISHOP];
        ulong whiteRooks = board.pieceBitboards[Piece.WHITE_ROOK];
        ulong whiteQueens = board.pieceBitboards[Piece.WHITE_QUEEN];

        ulong blackPawns = board.pieceBitboards[Piece.BLACK_PAWN];
        ulong blackKnights = board.pieceBitboards[Piece.BLACK_KNIGHT];
        ulong blackBishops = board.pieceBitboards[Piece.BLACK_BISHOP];
        ulong blackRooks = board.pieceBitboards[Piece.BLACK_ROOK];
        ulong blackQueens = board.pieceBitboards[Piece.BLACK_QUEEN];

        while (whitePawns != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref whitePawns);
            phase += PiecePhase[Piece.PAWN];
            eval += PieceValues[Piece.PAWN];
            earlyGameEval += PieceSquareTables.Read(PieceSquareTables.Pawns, index, true);
            lateGameEval += PieceSquareTables.Read(PieceSquareTables.PawnsEnd, index, true);
        }
        while (whiteKnights != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref whiteKnights);
            phase += PiecePhase[Piece.KNIGHT];
            eval += PieceValues[Piece.KNIGHT];
            eval += PieceSquareTables.Read(PieceSquareTables.Knights, index, true);
        }
        while (whiteBishops != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref whiteBishops);
            phase += PiecePhase[Piece.BISHOP];
            eval += PieceValues[Piece.BISHOP];
            eval += PieceSquareTables.Read(PieceSquareTables.Bishops, index, true);
        }
        while (whiteRooks != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref whiteRooks);
            phase += PiecePhase[Piece.ROOK];
            eval += PieceValues[Piece.ROOK];
            eval += PieceSquareTables.Read(PieceSquareTables.Rooks, index, true);
        }
        while (whiteQueens != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref whiteQueens);
            phase += PiecePhase[Piece.QUEEN];
            eval += PieceValues[Piece.QUEEN];
            eval += PieceSquareTables.Read(PieceSquareTables.Queens, index, true);
        }


        while (blackPawns != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref blackPawns);
            phase += PiecePhase[Piece.PAWN];
            eval -= PieceValues[Piece.PAWN];
            earlyGameEval -= PieceSquareTables.Read(PieceSquareTables.Pawns, index, false);
            lateGameEval -= PieceSquareTables.Read(PieceSquareTables.PawnsEnd, index, false);
        }
        while (blackKnights != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref blackKnights);
            phase += PiecePhase[Piece.KNIGHT];
            eval -= PieceValues[Piece.KNIGHT];
            eval -= PieceSquareTables.Read(PieceSquareTables.Knights, index, false);
        }
        while (blackBishops != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref blackBishops);
            phase += PiecePhase[Piece.BISHOP];
            eval -= PieceValues[Piece.BISHOP];
            eval -= PieceSquareTables.Read(PieceSquareTables.Bishops, index, false);
        }
        while (blackRooks != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref blackRooks);
            phase += PiecePhase[Piece.ROOK];
            eval -= PieceValues[Piece.ROOK];
            eval -= PieceSquareTables.Read(PieceSquareTables.Rooks, index, false);
        }
        while (blackQueens != 0)
        {
            int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref blackQueens);
            phase += PiecePhase[Piece.QUEEN];
            eval -= PieceValues[Piece.QUEEN];
            eval -= PieceSquareTables.Read(PieceSquareTables.Queens, index, false);
        }

        phase = Math.Min(phase, 24);
        eval += (earlyGameEval * (24 - phase) + lateGameEval * phase) / 24;

        return board.whiteToMove ? eval : -eval;
    }
}