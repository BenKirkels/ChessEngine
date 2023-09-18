using Helpers;

namespace Chess;

public readonly struct Move
{
    // ffff ssssss tttttt
    // f: flags (4bits)
    // s: from square (6bits)
    // t: to square (6bits)
    readonly ushort moveData;

    public const int SIZE_OF_MOVE = sizeof(ushort);

    // Masks
    public const ushort FLAG_MASK = 0b1111_000000_000000;
    public const ushort FROM_MASK = 0b0000_111111_000000;
    public const ushort TO_MASK = 0b0000_000000_111111;
    public const int PROMOTION_MASK = 0b1000;
    public const int CAPTURE_MASK = 0b0100;

    // Flags
    public const int QUIET_MOVE = 0b0000;
    public const int DOUBLE_PAWN_PUSH = 0b0001;
    public const int KING_CASTLE = 0b0010;
    public const int QUEEN_CASTLE = 0b0011;
    public const int CAPTURE = 0b0100;
    public const int EN_PASSANT = 0b0101;
    public const int QUEEN_PROMOTION = 0b1000;
    public const int KNIGHT_PROMOTION = 0b1001;
    public const int BISHOP_PROMOTION = 0b1010;
    public const int ROOK_PROMOTION = 0b1011;
    public const int QUEEN_PROMOTION_CAPTURE = 0b1100;
    public const int KNIGHT_PROMOTION_CAPTURE = 0b1101;
    public const int BISHOP_PROMOTION_CAPTURE = 0b1110;
    public const int ROOK_PROMOTION_CAPTURE = 0b1111;


    public Move(ushort moveData)
    {
        this.moveData = moveData;
    }

    public Move(int from, int to, int flag)
    {
        moveData = (ushort)((flag << 12) | (from << 6) | to);
    }

    public Move(string move, int flag)
    {
        string from = move.Substring(0, 2);
        string to = move.Substring(2, 2);
        moveData = (ushort)((flag << 12) | (BoardHelper.UCIToIndex(from) << 6) | BoardHelper.UCIToIndex(to));
    }

    public Move(string move, Board board)
    {
        string from = move.Substring(0, 2);
        string to = move.Substring(2, 2);

        int fromIndex = BoardHelper.UCIToIndex(from);
        int toIndex = BoardHelper.UCIToIndex(to);

        int movedPiece = Piece.PieceType(board.PieceAtSquare(fromIndex));
        int capturedPiece = Piece.PieceType(board.PieceAtSquare(toIndex));

        int flag = QUIET_MOVE;

        // Promotion
        if (move.Length == 5)
        {
            switch (move[4])
            {
                case 'q':
                    flag = QUEEN_PROMOTION;
                    break;
                case 'n':
                    flag = KNIGHT_PROMOTION;
                    break;
                case 'b':
                    flag = BISHOP_PROMOTION;
                    break;
                case 'r':
                    flag = ROOK_PROMOTION;
                    break;
            }
        }

        // Capture
        if (capturedPiece != Piece.NONE)
        {
            flag |= CAPTURE;
        }

        // En passant
        if (movedPiece == Piece.PAWN && capturedPiece == Piece.NONE && BoardHelper.IndexToFile(fromIndex) != BoardHelper.IndexToFile(toIndex))
        {
            flag = EN_PASSANT;
        }

        // Double pawn push
        if (movedPiece == Piece.PAWN && Math.Abs(fromIndex - toIndex) == 16)
        {
            flag = DOUBLE_PAWN_PUSH;
        }

        // Castling
        if (movedPiece == Piece.KING && Math.Abs(fromIndex - toIndex) == 2)
        {
            flag = BoardHelper.IndexToFile(toIndex) == 2 ? QUEEN_CASTLE : KING_CASTLE;
        }

        moveData = (ushort)((flag << 12) | (fromIndex << 6) | toIndex);
    }

    public int value => moveData;
    public int flag => (moveData & FLAG_MASK) >> 12;
    public int from => (moveData & FROM_MASK) >> 6;
    public int to => moveData & TO_MASK;

    public bool isPromotion => (flag & PROMOTION_MASK) != 0;
    public bool isEnPassant => flag == EN_PASSANT;
    public bool isCastle => flag == KING_CASTLE || flag == QUEEN_CASTLE;
    public bool isPawnDouble => flag == DOUBLE_PAWN_PUSH;
    public bool isCapture => (flag & CAPTURE_MASK) != 0;
    public int promotionPieceType => (flag & ~CAPTURE_MASK
    ) switch
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
        string moveString = BoardHelper.IndexToUCI(from) + BoardHelper.IndexToUCI(to);
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

    public static bool operator ==(Move a, Move b) => a.moveData == b.moveData;
    public static bool operator !=(Move a, Move b) => a.moveData != b.moveData;

}