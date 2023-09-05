namespace Chess;

public readonly struct Move
{
    // ffff ssssss tttttt
    // f: flags (4bits)
    // s: from square (6bits)
    // t: to square (6bits)
    readonly ushort moveData;

    // Masks
    public const ushort FLAG_MASK = 0b1111_000000_000000;
    public const ushort FROM_MASK = 0b0000_111111_000000;
    public const ushort TO_MASK = 0b0000_000000_111111;

    // Flags
    public const int NO_FLAGS = 0b0000;
    public const int EN_PASSANT = 0b0001;
    public const int CASTLE = 0b0010;
    public const int PAWN_DOUBLE = 0b0011;
    public const int QUEEN_PROMOTION = 0b1100;
    public const int KNIGHT_PROMOTION = 0b1101;
    public const int BISHOP_PROMOTION = 0b1110;
    public const int ROOK_PROMOTION = 0b1111;



    public Move(ushort moveData)
    {
        this.moveData = moveData;
    }

    public int value => moveData;
    public int flag => (moveData & FLAG_MASK) >> 12;
    public int moveFrom => (moveData & FROM_MASK) >> 6;
    public int moveTo => moveData & TO_MASK;

    public bool isPromotion => flag >= QUEEN_PROMOTION;
    public bool isEnPassant => flag == EN_PASSANT;
    public bool isCastle => flag == CASTLE;
    public bool isPawnDouble => flag == PAWN_DOUBLE;
    public int promotionPieceType => flag switch
    {
        QUEEN_PROMOTION => Piece.QUEEN,
        KNIGHT_PROMOTION => Piece.KNIGHT,
        BISHOP_PROMOTION => Piece.BISHOP,
        ROOK_PROMOTION => Piece.ROOK,
        _ => Piece.NONE
    };

    public static readonly Move NullMove = new(0);
    public bool isNullMove => moveData == 0;

    public override string ToString()
    {
        string moveString = BoardHelper.IndexToUCI(moveFrom) + BoardHelper.IndexToUCI(moveTo);
        if (isPromotion)
            moveString += promotionPieceType switch
            {
                Piece.QUEEN => "q",
                Piece.ROOK => "r",
                Piece.BISHOP => "b",
                Piece.KNIGHT => "n",
                _ => throw new ArgumentException("Invalid promotion piece type")
            };
        return moveString;
    }
}