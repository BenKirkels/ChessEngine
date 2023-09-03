namespace Chess;

/// <summary>
/// 16 bits representation of a move.
/// {ffffttttttssssss}
/// <para> f: flags </para>
/// <para> t: target square </para>
/// <para> s: start square </para>
/// <remarks>!!! Does not contain information about the move and capture piece types !!!</remarks>
/// </summary>
public readonly struct MoveData
{
    // Masks
    public const ushort StartSquareMask = 0b111111;
    public const ushort TargetSquareMask = 0b111111000000;
    public const ushort FlagsMask = 0b1111000000000000;
    // Flags
    public const int NoFlags = 0b0000;
    public const int EnPassantFlag = 0b0001;
    public const int CastlingFlag = 0b0010;
    public const int QueenPromotionFlag = 0b0011;
    public const int RookPromotionFlag = 0b0100;
    public const int BishopPromotionFlag = 0b0101;
    public const int KnightPromotionFlag = 0b0110;

    private readonly ushort moveValue;
    private int moveFlag => (moveValue & FlagsMask) >> 12;


    public int moveFrom => moveValue & StartSquareMask;
    public int moveTo => (moveValue & TargetSquareMask) >> 6;
    public bool isPromotion => moveFlag >= QueenPromotionFlag;

    public PieceType promotionPieceType => moveFlag switch
    {
        QueenPromotionFlag => PieceType.Queen,
        RookPromotionFlag => PieceType.Rook,
        BishopPromotionFlag => PieceType.Bishop,
        KnightPromotionFlag => PieceType.Knight,
        _ => PieceType.Null
    };
    public bool isEnPassant => moveFlag == EnPassantFlag;
    public bool isCastle => moveFlag == CastlingFlag;


    public MoveData(ushort moveValue)
    {
        this.moveValue = moveValue;
    }

    public MoveData(int moveFrom, int moveTo, int moveFlag)
    {
        this.moveValue = (ushort)(moveFrom | (moveTo << 6) | (moveFlag << 12));
    }
}