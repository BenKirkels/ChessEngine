using System.Numerics;
using System.Runtime.InteropServices;
using Helpers;
using Microsoft.VisualBasic;
using MoveGeneration;

namespace Chess;


/// <summary>
/// Class <c> Board </c> represents the chess board.
/// It contains:
/// <para> - The bitboards for each piece type and color. </para>
/// <para> - The castling rights. </para>
/// <para> - The en passant square. </para>
/// <para> - The ply count. </para>
/// </summary>
public class Board
{
    const int WHITE_INDEX = 0;
    const int BLACK_INDEX = 1;
    const ulong WHITE_SQUARES = 0x55AA55AA55AA55AA;
    const ulong BLACK_SQUARES = 0xAA55AA55AA55AA55;
    const string DEFAULT_STARTING_POSITION = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public const int MAX_MOVES = 256;
    readonly MoveGen moveGenerator;
    bool hasInCheckValue;
    bool cashedInCheckValue;

    public ulong[] pieceBitboards = new ulong[Piece.MAX_PIECE_NUMBER];
    public ulong[] colorBitboards = new ulong[2];
    public ulong[] orthogonalSliders = new ulong[2];
    public ulong[] diagonalSliders = new ulong[2];
    public ulong allPiecesBitboard;

    public int EnPassantFile => gameState.enPassantFile;

    public int plyCount;

    public bool whiteToMove;
    public int MyColor => whiteToMove ? Piece.WHITE : Piece.BLACK;
    public int MyColorIndex => whiteToMove ? WHITE_INDEX : BLACK_INDEX;
    public int OpponentColor => whiteToMove ? Piece.BLACK : Piece.WHITE;
    public int OpponentColorIndex => whiteToMove ? BLACK_INDEX : WHITE_INDEX;

    public bool CanCastleKingSide => gameState.CanCastleKingSide(whiteToMove);
    public bool CanCastleQueenSide => gameState.CanCastleQueenSide(whiteToMove);

    /// <summary>
    /// Store the data that is not easily reversible.
    /// </summary>
    Stack<GameState> previousGameStates;
    Stack<int> capturedPieces;
    public GameState gameState;
    ulong piecesZobristKey;

    public Span<Move> GetLegalMoves(bool generateQuietMoves = true)
    {
        Span<Move> moves = new Move[MAX_MOVES];
        moveGenerator.GenerateMoves(ref moves, this, generateQuietMoves);
        return moves;
    }

    public bool IsPieceAtSquare(int square, int piece) => BitBoardHelper.IsBitSet(pieceBitboards[piece], square);
    public bool IsPieceAtSquare(int square) => BitBoardHelper.IsBitSet(allPiecesBitboard, square);

    public int PieceAtSquare(int square)
    {
        for (int p = Piece.PAWN; p <= Piece.KING; p++)
        {
            int piece = Piece.MakePiece(p, Piece.WHITE);
            if (IsPieceAtSquare(square, piece))
                return piece;
            piece = Piece.MakePiece(p, Piece.BLACK);
            if (IsPieceAtSquare(square, piece))
                return piece;
        }
        return Piece.NONE;
    }

    public int MovePieceType(Move move) => Piece.PieceType(PieceAtSquare(move.from));
    public int CapturePieceType(Move move) => Piece.PieceType(PieceAtSquare(move.to));
    public bool FiftyMoveDraw()
    {
        return gameState.fiftyMoveCounter >= 100;
    }
    public bool IsRepetition()
    {
        foreach (GameState state in previousGameStates)
        {
            if (state.zobristKey == gameState.zobristKey && state.castlingRights == gameState.castlingRights && state.enPassantFile == gameState.enPassantFile)
                return true;
        }
        return false;
    }

    public bool IsInsufficientMaterial()
    {
        // If there are any pawns, the position is not a draw
        if (pieceBitboards[Piece.WHITE_PAWN] != 0) return false;
        if (pieceBitboards[Piece.BLACK_PAWN] != 0) return false;

        // If there are any queens or rooks, the position is not a draw
        if (orthogonalSliders[WHITE_INDEX] != 0) return false;
        if (orthogonalSliders[BLACK_INDEX] != 0) return false;

        bool whiteSideDraw = false;
        bool blackSideDraw = false;

        ulong whiteKnights = pieceBitboards[Piece.WHITE_KNIGHT];
        ulong whiteBishops = pieceBitboards[Piece.WHITE_BISHOP];

        if (whiteKnights != 0)
        {
            // If there are multiple knights, the position is not a draw
            if (BitOperations.PopCount(whiteKnights) > 1) return false;

            // Knight and bishop is not a draw
            if (whiteBishops != 0) return false;

            // Lone knight is a draw
            whiteSideDraw = true;
        }

        ulong blackKnights = pieceBitboards[Piece.BLACK_KNIGHT];
        ulong blackBishops = pieceBitboards[Piece.BLACK_BISHOP];

        if (blackKnights != 0)
        {
            // If there are multiple knights, the position is not a draw
            if (BitOperations.PopCount(blackKnights) > 1) return false;

            // Knight and bishop is not a draw
            if (blackBishops != 0) return false;

            // Lone knight is a draw
            blackSideDraw = true;
        }

        // Only bishops left
        if (whiteBishops != 0)
        {
            // Need both square colors to be able to mate
            if ((whiteBishops & WHITE_SQUARES) == 0 || (whiteBishops & BLACK_SQUARES) == 0) whiteSideDraw = true;
            else return false;
        }

        if (blackBishops != 0)
        {
            // Need both square colors to be able to mate
            if ((blackBishops & WHITE_SQUARES) == 0 || (blackBishops & BLACK_SQUARES) == 0) blackSideDraw = true;
            else return false;
        }

        if (whiteSideDraw && blackSideDraw) return true;
        return false;
    }

    public bool InCheck()
    {
        if (hasInCheckValue) return cashedInCheckValue;

        ulong kingBitboard = pieceBitboards[Piece.MakePiece(Piece.KING, MyColor)];
        int kingSquare = BitBoardHelper.ClearAndGetIndexOfLSB(ref kingBitboard);

        ulong opponentPawns = pieceBitboards[Piece.MakePiece(Piece.PAWN, OpponentColor)];
        ulong opponentKnights = pieceBitboards[Piece.MakePiece(Piece.KNIGHT, OpponentColor)];
        ulong opponentOrthogonalSliders = orthogonalSliders[OpponentColorIndex];
        ulong opponentDiagonalSliders = diagonalSliders[OpponentColorIndex];

        if (opponentPawns != 0 && (PrecomputedData.PawnAttacks(kingSquare, MyColor) & opponentPawns) != 0)
        {
            cashedInCheckValue = true;
            hasInCheckValue = true;
            return true;
        }
        if (opponentKnights != 0 && (PrecomputedData.knightMoves[kingSquare] & opponentKnights) != 0)
        {
            cashedInCheckValue = true;
            hasInCheckValue = true;
            return true;
        }
        if (opponentOrthogonalSliders != 0 && (PrecomputedData.RookMoves(kingSquare, allPiecesBitboard) & opponentOrthogonalSliders) != 0)
        {
            cashedInCheckValue = true;
            hasInCheckValue = true;
            return true;
        }
        if (opponentDiagonalSliders != 0 && (PrecomputedData.BishopMoves(kingSquare, allPiecesBitboard) & opponentDiagonalSliders) != 0)
        {
            cashedInCheckValue = true;
            hasInCheckValue = true;
            return true;
        }
        cashedInCheckValue = false;
        hasInCheckValue = true;
        return false;
    }
    public void MakeMove(Move move)
    {
        previousGameStates.Push(gameState);

        int enPassantFile = -1;
        int castlingRights = gameState.castlingRights;
        int fiftyMoveCounter = gameState.fiftyMoveCounter + 1;

        int movedPiece = PieceAtSquare(move.from);

        switch (move.flag)
        {
            case Move.QUIET_MOVE:
                switch (Piece.PieceType(movedPiece))
                {
                    case Piece.PAWN:
                        fiftyMoveCounter = 0; // Reset after pawn move
                        break;
                    case Piece.KING:
                        castlingRights &= whiteToMove ? 0b0011 : 0b1100; // Remove castling rights for the side that moved its king
                        break;
                    case Piece.ROOK: // Remove castling rights for the side that moved the rook
                        switch (move.from)
                        {
                            case 0:
                                castlingRights &= 0b1011;
                                break;
                            case 7:
                                castlingRights &= 0b0111;
                                break;
                            case 56:
                                castlingRights &= 0b1110;
                                break;
                            case 63:
                                castlingRights &= 0b1101;
                                break;
                        }
                        break;
                }

                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                break;
            case Move.DOUBLE_PAWN_PUSH:
                fiftyMoveCounter = 0; // Reset after pawn move

                enPassantFile = BoardHelper.IndexToFile(move.to);

                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                break;
            case Move.KING_CASTLE:
                // King movement
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Rook movement
                int myRook = Piece.MakePiece(Piece.ROOK, MyColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to + 1);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to - 1);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to + 1);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to - 1);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to + 1);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to - 1);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to + 1);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to - 1);

                castlingRights &= whiteToMove ? 0b0011 : 0b1100; // Remove castling rights for the side that castled
                break;
            case Move.QUEEN_CASTLE:
                // King movement
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Rook movement
                myRook = Piece.MakePiece(Piece.ROOK, MyColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to + 1);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to - 2);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to + 1);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to - 2);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to + 1);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to - 2);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to + 1);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to - 2);

                castlingRights &= whiteToMove ? 0b0011 : 0b1100; // Remove castling rights for the side that castled
                break;
            case Move.CAPTURE:
                fiftyMoveCounter = 0; // Reset after capture

                int capturedPiece = PieceAtSquare(move.to);

                // Moved piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Captured piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[capturedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[OpponentColorIndex], move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, capturedPiece, move.to);

                // If the captured piece is a rook, remove castling rights for the side that lost the rook
                if (Piece.PieceType(capturedPiece) == Piece.ROOK)
                {
                    switch (move.to)
                    {
                        case 0:
                            castlingRights &= 0b1011;
                            break;
                        case 7:
                            castlingRights &= 0b0111;
                            break;
                        case 56:
                            castlingRights &= 0b1110;
                            break;
                        case 63:
                            castlingRights &= 0b1101;
                            break;
                    }
                }

                switch (Piece.PieceType(movedPiece))
                {
                    case Piece.KING: // Remove castling rights for the side that moved its king
                        castlingRights &= whiteToMove ? 0b0011 : 0b1100;
                        break;
                    case Piece.ROOK: // Remove castling rights for the side that moved the rook
                        switch (move.from)
                        {
                            case 0:
                                castlingRights &= 0b1011;
                                break;
                            case 7:
                                castlingRights &= 0b0111;
                                break;
                            case 56:
                                castlingRights &= 0b1110;
                                break;
                            case 63:
                                castlingRights &= 0b1101;
                                break;
                        }
                        break;
                }

                capturedPieces.Push(capturedPiece); // Store the captured piece for undoing the move
                break;
            case Move.EN_PASSANT:
                fiftyMoveCounter = 0; // Reset after capture/pawn move

                // Moved piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Captured piece
                int capturedPawnIndex = move.to + (whiteToMove ? -8 : 8);
                int opponentPawn = Piece.MakePiece(Piece.PAWN, OpponentColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[opponentPawn], capturedPawnIndex);

                BitBoardHelper.ToggleIndex(ref colorBitboards[OpponentColorIndex], capturedPawnIndex);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, capturedPawnIndex);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, opponentPawn, capturedPawnIndex);

                break;
            case Move.QUEEN_PROMOTION:
            case Move.KNIGHT_PROMOTION:
            case Move.BISHOP_PROMOTION:
            case Move.ROOK_PROMOTION:
                fiftyMoveCounter = 0; // Reset after pawn move

                int promotionPiece = Piece.MakePiece(move.promotionPieceType, MyColor);

                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[promotionPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, promotionPiece, move.to);

                break;
            case Move.QUEEN_PROMOTION_CAPTURE:
            case Move.KNIGHT_PROMOTION_CAPTURE:
            case Move.BISHOP_PROMOTION_CAPTURE:
            case Move.ROOK_PROMOTION_CAPTURE:
                fiftyMoveCounter = 0; // Reset after capture/pawn move

                capturedPiece = PieceAtSquare(move.to);
                promotionPiece = Piece.MakePiece(move.promotionPieceType, MyColor);

                // Moved piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[promotionPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, promotionPiece, move.to);

                // Captured piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[capturedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[OpponentColorIndex], move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, capturedPiece, move.to);


                // If the captured piece is a rook, remove castling rights for the side that lost the rook
                if (Piece.PieceType(capturedPiece) == Piece.ROOK)
                {
                    switch (move.to)
                    {
                        case 0:
                            castlingRights &= 0b1011;
                            break;
                        case 7:
                            castlingRights &= 0b0111;
                            break;
                        case 56:
                            castlingRights &= 0b1110;
                            break;
                        case 63:
                            castlingRights &= 0b1101;
                            break;
                    }
                }

                capturedPieces.Push(capturedPiece); // Store the captured piece for undoing the move
                break;
        }
        orthogonalSliders[MyColorIndex] = pieceBitboards[Piece.MakePiece(Piece.ROOK, MyColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, MyColor)];
        diagonalSliders[MyColorIndex] = pieceBitboards[Piece.MakePiece(Piece.BISHOP, MyColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, MyColor)];

        orthogonalSliders[OpponentColorIndex] = pieceBitboards[Piece.MakePiece(Piece.ROOK, OpponentColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, OpponentColor)];
        diagonalSliders[OpponentColorIndex] = pieceBitboards[Piece.MakePiece(Piece.BISHOP, OpponentColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, OpponentColor)];


        whiteToMove = !whiteToMove;
        Zobrist.SwitchColor(ref piecesZobristKey);

        ulong zobristKey = piecesZobristKey ^ Zobrist.GenerateGameStateZobristKey(castlingRights, enPassantFile);
        gameState = new GameState(castlingRights, enPassantFile, zobristKey, fiftyMoveCounter);
        plyCount++;

        hasInCheckValue = false;
    }

    public void UndoMove(Move move)
    {
        gameState = previousGameStates.Pop();
        whiteToMove = !whiteToMove;
        Zobrist.SwitchColor(ref piecesZobristKey);
        plyCount--;

        int movedPiece = PieceAtSquare(move.to);

        switch (move.flag)
        {
            case Move.QUIET_MOVE:
            case Move.DOUBLE_PAWN_PUSH:

                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                break;
            case Move.KING_CASTLE:
                // King movement
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Rook movement
                int myRook = Piece.MakePiece(Piece.ROOK, MyColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to + 1);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to - 1);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to + 1);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to - 1);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to + 1);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to - 1);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to + 1);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to - 1);

                break;
            case Move.QUEEN_CASTLE:
                // King movement
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Rook movement
                myRook = Piece.MakePiece(Piece.ROOK, MyColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to + 1);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[myRook], move.to - 2);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to + 1);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to - 2);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to + 1);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to - 2);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to + 1);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, myRook, move.to - 2);

                break;
            case Move.CAPTURE:
                int capturedPiece = capturedPieces.Pop();

                // Moved piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Captured piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[capturedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[OpponentColorIndex], move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, capturedPiece, move.to);

                break;
            case Move.EN_PASSANT:
                // Moved piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.to);

                // Captured piece
                int capturedPawnIndex = move.to + (whiteToMove ? -8 : 8);
                int opponentPawn = Piece.MakePiece(Piece.PAWN, OpponentColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[opponentPawn], capturedPawnIndex);

                BitBoardHelper.ToggleIndex(ref colorBitboards[OpponentColorIndex], capturedPawnIndex);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, capturedPawnIndex);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, opponentPawn, capturedPawnIndex);

                break;
            case Move.QUEEN_PROMOTION:
            case Move.KNIGHT_PROMOTION:
            case Move.BISHOP_PROMOTION:
            case Move.ROOK_PROMOTION:
                int promotionPiece = movedPiece;
                movedPiece = Piece.MakePiece(Piece.PAWN, MyColor);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[promotionPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);
                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, promotionPiece, move.to);

                break;
            case Move.QUEEN_PROMOTION_CAPTURE:
            case Move.KNIGHT_PROMOTION_CAPTURE:
            case Move.BISHOP_PROMOTION_CAPTURE:
            case Move.ROOK_PROMOTION_CAPTURE:
                capturedPiece = capturedPieces.Pop();
                promotionPiece = movedPiece;
                movedPiece = Piece.MakePiece(Piece.PAWN, MyColor);

                // Moved piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[movedPiece], move.from);
                BitBoardHelper.ToggleIndex(ref pieceBitboards[promotionPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.from);
                BitBoardHelper.ToggleIndex(ref colorBitboards[MyColorIndex], move.to);

                BitBoardHelper.ToggleIndex(ref allPiecesBitboard, move.from);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, movedPiece, move.from);
                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, promotionPiece, move.to);

                // Captured piece
                BitBoardHelper.ToggleIndex(ref pieceBitboards[capturedPiece], move.to);

                BitBoardHelper.ToggleIndex(ref colorBitboards[OpponentColorIndex], move.to);

                Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, capturedPiece, move.to);

                break;
        }
        orthogonalSliders[MyColorIndex] = pieceBitboards[Piece.MakePiece(Piece.ROOK, MyColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, MyColor)];
        diagonalSliders[MyColorIndex] = pieceBitboards[Piece.MakePiece(Piece.BISHOP, MyColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, MyColor)];

        orthogonalSliders[OpponentColorIndex] = pieceBitboards[Piece.MakePiece(Piece.ROOK, OpponentColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, OpponentColor)];
        diagonalSliders[OpponentColorIndex] = pieceBitboards[Piece.MakePiece(Piece.BISHOP, OpponentColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, OpponentColor)];
    }

    public Board()
    {
        moveGenerator = new MoveGen();
        previousGameStates = new Stack<GameState>();
        capturedPieces = new Stack<int>();

        pieceBitboards = new ulong[12];
        colorBitboards = new ulong[2];
        orthogonalSliders = new ulong[2];
        diagonalSliders = new ulong[2];
        allPiecesBitboard = 0;

        whiteToMove = true;
        gameState = new GameState();
        plyCount = 0;
        piecesZobristKey = 0;

        hasInCheckValue = false;
    }

    public void SetPosition(string fen = DEFAULT_STARTING_POSITION)
    {
        string[] fenParts = fen.Split(' ');
        if (fenParts.Length != 6)
            throw new ArgumentException("Invalid FEN string: not all parts are present");

        // Piece placement
        pieceBitboards = FenHelper.BitBoards(fenParts[0]);

        colorBitboards[WHITE_INDEX] = pieceBitboards[Piece.WHITE_PAWN] | pieceBitboards[Piece.WHITE_KNIGHT] | pieceBitboards[Piece.WHITE_BISHOP] | pieceBitboards[Piece.WHITE_ROOK] | pieceBitboards[Piece.WHITE_QUEEN] | pieceBitboards[Piece.WHITE_KING];
        colorBitboards[BLACK_INDEX] = pieceBitboards[Piece.BLACK_PAWN] | pieceBitboards[Piece.BLACK_KNIGHT] | pieceBitboards[Piece.BLACK_BISHOP] | pieceBitboards[Piece.BLACK_ROOK] | pieceBitboards[Piece.BLACK_QUEEN] | pieceBitboards[Piece.BLACK_KING];

        orthogonalSliders[WHITE_INDEX] = pieceBitboards[Piece.WHITE_ROOK] | pieceBitboards[Piece.WHITE_QUEEN];
        orthogonalSliders[BLACK_INDEX] = pieceBitboards[Piece.BLACK_ROOK] | pieceBitboards[Piece.BLACK_QUEEN];

        diagonalSliders[WHITE_INDEX] = pieceBitboards[Piece.WHITE_BISHOP] | pieceBitboards[Piece.WHITE_QUEEN];
        diagonalSliders[BLACK_INDEX] = pieceBitboards[Piece.BLACK_BISHOP] | pieceBitboards[Piece.BLACK_QUEEN];

        allPiecesBitboard = colorBitboards[WHITE_INDEX] | colorBitboards[BLACK_INDEX];

        // Active color
        whiteToMove = fenParts[1] == "w";

        // Castling rights
        int castlingRights = FenHelper.CastlingRights(fenParts[2]);

        // En passant square
        int enPassantFile = FenHelper.EnPassantFile(fenParts[3]);

        // Zobrist key
        piecesZobristKey = Zobrist.GeneratePiecesZobristKey(pieceBitboards, whiteToMove);
        ulong zobristKey = piecesZobristKey ^ Zobrist.GenerateGameStateZobristKey(castlingRights, enPassantFile);

        gameState = new GameState(
            castlingRights,
            enPassantFile,
            zobristKey,
            // 50 move counter
            fenParts[4] == "-" ? 0 : int.Parse(fenParts[4])
        );

        // Ply count
        // The fen string contains the number of the full moves
        plyCount = 2 * (int.Parse(fenParts[5]) - 1) + (whiteToMove ? 0 : 1);

        previousGameStates.Clear();
        capturedPieces.Clear();
        hasInCheckValue = false;
    }

    /// <summary>
    /// Initializes a new instance of the <c> Board </c> class.
    /// </summary>
    /// <param name="fen"></param>
    /// <returns> <c>board</c> object </returns>
    /// <exception cref="ArgumentException"></exception>
    public static Board CreateBoardFromFen(string fen)
    {
        Board board = new Board();
        board.SetPosition(fen);
        return board;
    }


}
