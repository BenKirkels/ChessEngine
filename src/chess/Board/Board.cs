using System.Text;

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
    private MoveGen moveGenerator = new();
    public ulong[] pieceBitboards;
    public ulong[] colorBitboards;
    public ulong allPiecesBitboard;

    public ulong enPassantSquare => BitBoardHelper.Index(gameState.enPassantSquare);

    public int plyCount;

    public bool whiteToMove;
    public int myColor => whiteToMove ? Piece.WHITE : Piece.BLACK;
    public int opponentColor => whiteToMove ? Piece.BLACK : Piece.WHITE;


    /// <summary>
    /// Store the data that is not easily reversible.
    /// </summary>
    private Stack<GameState> previousGameStates;
    public GameState gameState;

    public void GetLegalMoves(ref Span<Move> moves, bool ignoreQuietMoves = false) => moveGenerator.GenerateMoves(ref moves, this, ignoreQuietMoves);

    public bool PieceAtSquare(int piece, int square) => (pieceBitboards[piece] &= BitBoardHelper.Index(square)) != 0;


    /// <summary>
    /// Initializes a new instance of the <c> Board </c> class.
    /// </summary>
    /// <param name="fen"></param>
    /// <returns> <c>board</c> object </returns>
    /// <exception cref="ArgumentException"></exception>
    public static Board CreateBoardFromFen(string fen)
    {
        Board board = new();
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
        int enPassantSquare = FenHelper.EnPassantSquare(fenParts[3]);

        // Zobrist key
        ulong zobristKey = Zobrist.GenerateZobristKey(bitboards, castlingRights, enPassantSquare, whiteToMove);


        board.pieceBitboards = bitboards;
        board.gameState = new GameState(
            castlingRights,
            enPassantSquare,
            zobristKey,
            // 50 move counter
            fenParts[4] == "-" ? 0 : int.Parse(fenParts[4])
        );
        board.whiteToMove = whiteToMove;

        // Ply count
        // The fen string contains the number of the full moves
        board.plyCount = 2 * (int.Parse(fenParts[5]) - 1) + (board.whiteToMove ? 0 : 1);
        return board;
    }


}
