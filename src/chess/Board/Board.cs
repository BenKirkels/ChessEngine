using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.InteropServices;
using Evaluation;
using Helpers;
using Microsoft.VisualBasic;
using MoveGeneration;
using Search;

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
    public const int WHITE_INDEX = 0;
    public const int BLACK_INDEX = 1;
    const ulong WHITE_SQUARES = 0x55AA55AA55AA55AA;
    const ulong BLACK_SQUARES = 0xAA55AA55AA55AA55;
    const string DEFAULT_STARTING_POSITION = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public const int MAX_MOVES = 256;
    readonly MoveGen moveGenerator;
    bool hasInCheckValue;
    bool cashedInCheckValue;

    public ulong[] pieceBitboards;
    public ulong[] colorBitboards;
    public ulong[] orthogonalSliders;
    public ulong[] diagonalSliders;
    public ulong allPiecesBitboard;

    public int[] squares;

    public int[] pieceCount;
    public int[] pcsqMg;
    public int[] pcsqEg;

    public int EnPassantFile => gameState.enPassantFile;

    public int plyCount;

    public bool whiteToMove;
    public int MyColor => whiteToMove ? Piece.WHITE : Piece.BLACK;
    public int MyColorIndex => whiteToMove ? WHITE_INDEX : BLACK_INDEX;
    public int OpponentColor => whiteToMove ? Piece.BLACK : Piece.WHITE;
    public int OpponentColorIndex => whiteToMove ? BLACK_INDEX : WHITE_INDEX;

    public bool CanCastleKingSide => gameState.CanCastleKingSide(whiteToMove);
    public bool CanCastleQueenSide => gameState.CanCastleQueenSide(whiteToMove);
    public bool CanCastle(int color) => gameState.CanCastle(color);

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

    public int PieceAtIndex(int index)
    {
        return squares[index];
    }

    public int MovePieceType(Move move) => Piece.PieceType(PieceAtIndex(move.from));
    public int CapturePieceType(Move move) => Piece.PieceType(PieceAtIndex(move.to));
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

        int movedPiece = ClearSquare(move.from);

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
                FillSquare(move.to, movedPiece);

                break;
            case Move.DOUBLE_PAWN_PUSH:
                fiftyMoveCounter = 0; // Reset after pawn move

                enPassantFile = BoardHelper.IndexToFile(move.to);

                FillSquare(move.to, movedPiece);
                break;
            case Move.KING_CASTLE:
                // King movement
                FillSquare(move.to, movedPiece);
                // Rook movement
                int myRook = ClearSquare(move.to + 1);
                FillSquare(move.to - 1, myRook);

                castlingRights &= whiteToMove ? 0b0011 : 0b1100; // Remove castling rights for the side that castled
                break;
            case Move.QUEEN_CASTLE:
                // King movement
                FillSquare(move.to, movedPiece);
                // Rook movement
                myRook = ClearSquare(move.to - 2);
                FillSquare(move.to + 1, myRook);

                castlingRights &= whiteToMove ? 0b0011 : 0b1100; // Remove castling rights for the side that castled
                break;
            case Move.CAPTURE:
                fiftyMoveCounter = 0; // Reset after capture

                int capturedPiece = ReplaceSquare(move.to, movedPiece);
                capturedPieces.Push(capturedPiece); // Store the captured piece for undoing the move

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
                break;
            case Move.EN_PASSANT:
                fiftyMoveCounter = 0; // Reset after capture/pawn move

                // Moved piece
                FillSquare(move.to, movedPiece);
                // Captured piece
                ClearSquare(move.to + (whiteToMove ? -8 : 8));

                break;
            case Move.QUEEN_PROMOTION:
            case Move.KNIGHT_PROMOTION:
            case Move.BISHOP_PROMOTION:
            case Move.ROOK_PROMOTION:
                fiftyMoveCounter = 0; // Reset after pawn move

                int promotionPiece = Piece.MakePiece(move.promotionPieceType, MyColor);

                FillSquare(move.to, promotionPiece);

                break;
            case Move.QUEEN_PROMOTION_CAPTURE:
            case Move.KNIGHT_PROMOTION_CAPTURE:
            case Move.BISHOP_PROMOTION_CAPTURE:
            case Move.ROOK_PROMOTION_CAPTURE:
                fiftyMoveCounter = 0; // Reset after capture/pawn move

                promotionPiece = Piece.MakePiece(move.promotionPieceType, MyColor);

                capturedPiece = ReplaceSquare(move.to, promotionPiece);
                capturedPieces.Push(capturedPiece); // Store the captured piece for undoing the move

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

        switch (move.flag)
        {
            case Move.QUIET_MOVE:
            case Move.DOUBLE_PAWN_PUSH:
                int movedPiece = ClearSquare(move.to);
                FillSquare(move.from, movedPiece);
                break;
            case Move.KING_CASTLE:
                // King movement
                movedPiece = ClearSquare(move.to);
                FillSquare(move.from, movedPiece);
                // Rook movement
                int myRook = ClearSquare(move.to - 1);
                FillSquare(move.to + 1, myRook);
                break;
            case Move.QUEEN_CASTLE:
                // King movement
                movedPiece = ClearSquare(move.to);
                FillSquare(move.from, movedPiece);
                // Rook movement
                myRook = ClearSquare(move.to + 1);
                FillSquare(move.to - 2, myRook);
                break;
            case Move.CAPTURE:
                int capturedPiece = capturedPieces.Pop();
                movedPiece = ReplaceSquare(move.to, capturedPiece);
                FillSquare(move.from, movedPiece);
                break;
            case Move.EN_PASSANT:
                movedPiece = ClearSquare(move.to);
                FillSquare(move.from, movedPiece);
                FillSquare(move.to + (whiteToMove ? -8 : 8), Piece.MakePiece(Piece.PAWN, OpponentColor));
                break;
            case Move.QUEEN_PROMOTION:
            case Move.KNIGHT_PROMOTION:
            case Move.BISHOP_PROMOTION:
            case Move.ROOK_PROMOTION:
                ClearSquare(move.to);
                FillSquare(move.from, Piece.MakePiece(Piece.PAWN, MyColor));
                break;
            case Move.QUEEN_PROMOTION_CAPTURE:
            case Move.KNIGHT_PROMOTION_CAPTURE:
            case Move.BISHOP_PROMOTION_CAPTURE:
            case Move.ROOK_PROMOTION_CAPTURE:
                capturedPiece = capturedPieces.Pop();
                ReplaceSquare(move.to, capturedPiece);
                FillSquare(move.from, Piece.MakePiece(Piece.PAWN, MyColor));
                break;
        }
        orthogonalSliders[MyColorIndex] = pieceBitboards[Piece.MakePiece(Piece.ROOK, MyColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, MyColor)];
        diagonalSliders[MyColorIndex] = pieceBitboards[Piece.MakePiece(Piece.BISHOP, MyColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, MyColor)];

        orthogonalSliders[OpponentColorIndex] = pieceBitboards[Piece.MakePiece(Piece.ROOK, OpponentColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, OpponentColor)];
        diagonalSliders[OpponentColorIndex] = pieceBitboards[Piece.MakePiece(Piece.BISHOP, OpponentColor)] | pieceBitboards[Piece.MakePiece(Piece.QUEEN, OpponentColor)];
    }

    int ClearSquare(int index)
    {
        int piece = squares[index];
        int color = Piece.IsWhite(piece) ? WHITE_INDEX : BLACK_INDEX;

        BitBoardHelper.ToggleIndex(ref pieceBitboards[piece], index);
        BitBoardHelper.ToggleIndex(ref colorBitboards[color], index);
        BitBoardHelper.ToggleIndex(ref allPiecesBitboard, index);

        squares[index] = Piece.NONE;

        int pieceType = Piece.PieceType(piece);
        pieceCount[piece]--;
        pcsqMg[color] -= PieceSquareTables.GetValue(pieceType, index, false, color == WHITE_INDEX);
        pcsqEg[color] -= PieceSquareTables.GetValue(pieceType, index, true, color == WHITE_INDEX);

        Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, piece, index);

        return piece;
    }
    void FillSquare(int index, int piece)
    {
        bool isWhite = Piece.IsWhite(piece);
        int color = isWhite ? WHITE_INDEX : BLACK_INDEX;

        BitBoardHelper.ToggleIndex(ref pieceBitboards[piece], index);
        BitBoardHelper.ToggleIndex(ref colorBitboards[color], index);
        BitBoardHelper.ToggleIndex(ref allPiecesBitboard, index);

        squares[index] = piece;

        int pieceType = Piece.PieceType(piece);
        pieceCount[piece]++;
        pcsqMg[color] += PieceSquareTables.GetValue(pieceType, index, false, isWhite);
        pcsqEg[color] += PieceSquareTables.GetValue(pieceType, index, true, isWhite);

        Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, piece, index);
    }

    int ReplaceSquare(int index, int newPiece)
    {
        int oldPiece = squares[index];

        BitBoardHelper.ToggleIndex(ref pieceBitboards[oldPiece], index);
        BitBoardHelper.ToggleIndex(ref pieceBitboards[newPiece], index);
        BitBoardHelper.ToggleIndex(ref colorBitboards[WHITE_INDEX], index);
        BitBoardHelper.ToggleIndex(ref colorBitboards[BLACK_INDEX], index);

        squares[index] = newPiece;

        int oldPieceType = Piece.PieceType(oldPiece);
        bool oldIsWhite = Piece.IsWhite(oldPiece);
        int oldColor = oldIsWhite ? WHITE_INDEX : BLACK_INDEX;

        pieceCount[oldPiece]--;
        pcsqMg[oldColor] -= PieceSquareTables.GetValue(oldPieceType, index, false, oldIsWhite);
        pcsqEg[oldColor] -= PieceSquareTables.GetValue(oldPieceType, index, true, oldIsWhite);

        int newPieceType = Piece.PieceType(newPiece);
        bool newIsWhite = Piece.IsWhite(newPiece);
        int newColor = newIsWhite ? WHITE_INDEX : BLACK_INDEX;

        pieceCount[newPiece]++;
        pcsqMg[newColor] += PieceSquareTables.GetValue(newPieceType, index, false, newIsWhite);
        pcsqEg[newColor] += PieceSquareTables.GetValue(newPieceType, index, true, newIsWhite);


        Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, oldPiece, index);
        Zobrist.UpdatePiecesZobristKey(ref piecesZobristKey, newPiece, index);

        return oldPiece;
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
        squares = new int[64];

        pieceCount = new int[Piece.MAX_PIECE_NUMBER + 1];
        pcsqMg = new int[2];
        pcsqEg = new int[2];

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

        squares = new int[64];
        pieceCount = new int[Piece.MAX_PIECE_NUMBER + 1];
        pcsqMg = new int[2];
        pcsqEg = new int[2];
        foreach (int piece in Piece.PieceNumbers)
        {
            bool isWhite = Piece.IsWhite(piece);
            int colorIndex = isWhite ? WHITE_INDEX : BLACK_INDEX;
            int pieceType = Piece.PieceType(piece);

            ulong bitboard = pieceBitboards[piece];
            while (bitboard != 0)
            {
                int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref bitboard);
                squares[index] = piece;

                pieceCount[piece]++;
                pcsqMg[colorIndex] += PieceSquareTables.GetValue(pieceType, index, false, isWhite);
                pcsqEg[colorIndex] += PieceSquareTables.GetValue(pieceType, index, true, isWhite);
            }
        }

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
