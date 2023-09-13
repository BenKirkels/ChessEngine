

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
    private readonly MoveGen moveGenerator;
    public ulong[] pieceBitboards;
    public ulong[] colorBitboards;
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
    private Stack<GameState> previousGameStates;
    public GameState gameState;

    public void GetLegalMoves(ref Span<Move> moves, bool generateQuietMoves = true) => moveGenerator.GenerateMoves(ref moves, this, generateQuietMoves);

    public bool PieceAtSquare(int square, int piece) => (pieceBitboards[piece] &= BitBoardHelper.Index(square)) != 0;
    public bool PieceAtSquare(int square) => BitBoardHelper.IsBitSet(allPiecesBitboard, square);


    public Board(ulong[] BitBoards, bool whiteToMove, GameState gameState, int plyCount)
    {
        moveGenerator = new MoveGen();
        previousGameStates = new Stack<GameState>();

        pieceBitboards = BitBoards;
        colorBitboards = new ulong[2];
        colorBitboards[WHITE_INDEX] = BitBoards[Piece.WHITE_PAWN] | BitBoards[Piece.WHITE_KNIGHT] | BitBoards[Piece.WHITE_BISHOP] | BitBoards[Piece.WHITE_ROOK] | BitBoards[Piece.WHITE_QUEEN] | BitBoards[Piece.WHITE_KING];
        colorBitboards[BLACK_INDEX] = BitBoards[Piece.BLACK_PAWN] | BitBoards[Piece.BLACK_KNIGHT] | BitBoards[Piece.BLACK_BISHOP] | BitBoards[Piece.BLACK_ROOK] | BitBoards[Piece.BLACK_QUEEN] | BitBoards[Piece.BLACK_KING];
        allPiecesBitboard = colorBitboards[WHITE_INDEX] | colorBitboards[BLACK_INDEX];

        this.whiteToMove = whiteToMove;
        this.gameState = gameState;
        this.plyCount = plyCount;
    }

    /// <summary>
    /// Initializes a new instance of the <c> Board </c> class.
    /// </summary>
    /// <param name="fen"></param>
    /// <returns> <c>board</c> object </returns>
    /// <exception cref="ArgumentException"></exception>
    public static Board CreateBoardFromFen(string fen)
    {
        string[] fenParts = fen.Split(' ');
        if (fenParts.Length != 6)
            throw new ArgumentException("Invalid FEN string: not all parts are present");

        // Piece placement
        ulong[] bitboards = FenHelper.BitBoards(fenParts[0]);

        // Active color
        bool whiteToMove = fenParts[1] == "w";

        // Castling rights
        int castlingRights = FenHelper.CastlingRights(fenParts[2]);

        // En passant square
        int enPassantFile = FenHelper.EnPassantFile(fenParts[3]);

        // Zobrist key
        ulong zobristKey = Zobrist.GenerateZobristKey(bitboards, castlingRights, enPassantFile, whiteToMove);

        GameState gameState = new GameState(
            castlingRights,
            enPassantFile,
            zobristKey,
            // 50 move counter
            fenParts[4] == "-" ? 0 : int.Parse(fenParts[4])
        );

        // Ply count
        // The fen string contains the number of the full moves
        int plyCount = 2 * (int.Parse(fenParts[5]) - 1) + (whiteToMove ? 0 : 1);
        Board board = new Board(bitboards, whiteToMove, gameState, plyCount);
        return board;
    }


}
