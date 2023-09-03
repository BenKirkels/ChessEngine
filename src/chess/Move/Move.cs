namespace Chess;

public readonly struct Move
{
    private readonly MoveData moveData;
    private readonly PiecetypeData piecetypeData;

    public Move(MoveData moveData, PiecetypeData piecetypeData)
    {
        this.moveData = moveData;
        this.piecetypeData = piecetypeData;
    }

    public int moveFrom => moveData.moveFrom;
    public int moveTo => moveData.moveTo;
    public PieceType movePieceType => piecetypeData.movePieceType;
    public PieceType capturePieceType => piecetypeData.capturePieceType;
    public bool isPromotion => moveData.isPromotion;
    public PieceType promotionPieceType => moveData.promotionPieceType;
    public bool isEnPassant => moveData.isEnPassant;
    public bool isCastle => moveData.isCastle;
    public bool isCapture => piecetypeData.capturePieceType != PieceType.Null;

    public override string ToString()
    {
        string moveString = BoardHelper.IndexToUCI(moveFrom) + BoardHelper.IndexToUCI(moveTo);
        if (isPromotion)
            moveString += promotionPieceType switch
            {
                PieceType.Queen => "q",
                PieceType.Rook => "r",
                PieceType.Bishop => "b",
                PieceType.Knight => "n",
                _ => throw new ArgumentException("Invalid promotion piece type")
            };
        return moveString;
    }
}