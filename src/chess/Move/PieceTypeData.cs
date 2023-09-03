namespace Chess;

/// <summary>
/// 6 bit representation of the move and capture pieceTypes.
/// {mmmccc}
/// <para> m: move pieceType </para>
/// <para> c: capture pieceType </para>
/// </summary>
public readonly struct PiecetypeData
{
    // Masks
    public const byte MovePieceTypeMask = 0b111000;
    public const byte CapturePieceTypeMask = 0b111;
    private readonly byte pieceTypesValue;

    public PieceType movePieceType => (PieceType)((pieceTypesValue & MovePieceTypeMask) >> 3);
    public PieceType capturePieceType => (PieceType)(pieceTypesValue & CapturePieceTypeMask);

    public PiecetypeData(byte pieceTypesValue)
    {
        this.pieceTypesValue = pieceTypesValue;
    }
    public PiecetypeData(PieceType movePieceType, PieceType capturePieceType)
    {
        this.pieceTypesValue = (byte)((int)movePieceType << 3 | (int)capturePieceType);
    }
}