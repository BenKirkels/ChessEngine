namespace Chess;
public static class Piece
{
    // Piece types
    public const int NONE = 0;
    public const int PAWN = 1;
    public const int KNIGHT = 2;
    public const int BISHOP = 3;
    public const int ROOK = 4;
    public const int QUEEN = 5;
    public const int KING = 6;

    // Colors
    public const int WHITE = 0;
    public const int BLACK = 8;

    // Pieces
    public const int WHITE_PAWN = PAWN | WHITE;
    public const int WHITE_KNIGHT = KNIGHT | WHITE;
    public const int WHITE_BISHOP = BISHOP | WHITE;
    public const int WHITE_ROOK = ROOK | WHITE;
    public const int WHITE_QUEEN = QUEEN | WHITE;
    public const int WHITE_KING = KING | WHITE;
    public const int BLACK_PAWN = PAWN | BLACK;
    public const int BLACK_KNIGHT = KNIGHT | BLACK;
    public const int BLACK_BISHOP = BISHOP | BLACK;
    public const int BLACK_ROOK = ROOK | BLACK;
    public const int BLACK_QUEEN = QUEEN | BLACK;
    public const int BLACK_KING = KING | BLACK;

    public const int MAX_PIECE_NUMBER = BLACK_KING;

    public static readonly int[] PieceNumbers = {
        WHITE_PAWN, WHITE_KNIGHT, WHITE_BISHOP, WHITE_ROOK, WHITE_QUEEN, WHITE_KING,
        BLACK_PAWN, BLACK_KNIGHT, BLACK_BISHOP, BLACK_ROOK, BLACK_QUEEN, BLACK_KING
    };


    public const int PIECE_TYPE_MASK = 0b111;
    public const int COLOR_MASK = 0b1000;

    public static int MakePiece(int pieceType, int color) => pieceType | color;
    public static int MakePiece(int pieceType, bool whitePiece) => pieceType | (whitePiece ? WHITE : BLACK);

    public static int PieceType(int piece) => piece & PIECE_TYPE_MASK;
}
