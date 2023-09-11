namespace Chess;
/// <summary>
/// Class <c> Zobrist </c> can be used to generate a zobrist key for a given board.
/// It uses:
/// <para> - A number for each piece type and color at each square. </para>
/// <para> - A number for every combination of castling rights. </para>
/// <para> - A number for each file that can be en passant. </para>
/// <para> - A number for side to move. </para>
/// </summary>
public static class Zobrist
{
    /// <summary>
    /// One number for each piece type and color at each square.
    /// </summary>
    private readonly static ulong[,] piecesNumbers = new ulong[12, 64];

    /// <summary>
    /// One number for every combination of castling rights.
    /// </summary>
    private readonly static ulong[] castlingRightsNumbers = new ulong[16];

    /// <summary>
    /// One number for each file that can be en passant.
    /// </summary>
    private readonly static ulong[] enPassantNumbers = new ulong[8];

    /// <summary>
    /// One number for side to move.
    /// </summary>
    private readonly static ulong whiteToMoveNumber = 0;

    /// <summary>
    /// Generates the random numbers used to generate the zobrist key.
    /// </summary>
    static Zobrist()
    {
        Random random = new Random(29426028);

        // Pieces
        for (int i = 0; i < 12; i++)
            for (int j = 0; j < 64; j++)
                piecesNumbers[i, j] = Random64bitNumber(random);

        // Castling rights
        for (int i = 0; i < 16; i++)
            castlingRightsNumbers[i] = Random64bitNumber(random);

        // En passant
        for (int i = 0; i < 8; i++)
            enPassantNumbers[i] = Random64bitNumber(random);

        // Side to move
        whiteToMoveNumber = Random64bitNumber(random);
    }

    /// <returns> A random 64-bit number </returns>
    public static ulong Random64bitNumber(Random random)
    {
        byte[] buffer = new byte[8];
        random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    public static ulong GenerateZobristKey(ulong[] pieceBitBoards, int castlingRights, int enPassantSquare, bool whiteToMove)
    {
        ulong zobristKey = 0;

        // Pieces
        for (int i = 0; i < 12; i++)
        {
            ulong pieceBitBoard = pieceBitBoards[i];
            while (pieceBitBoard != 0)
            {
                int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref pieceBitBoard);
                zobristKey ^= piecesNumbers[i, index];
            }
        }

        // Castling rights
        zobristKey ^= castlingRightsNumbers[castlingRights];

        // En passant
        if (enPassantSquare != -1)
            zobristKey ^= enPassantNumbers[BoardHelper.IndexToFile(enPassantSquare)];

        // Side to move
        if (whiteToMove)
            zobristKey ^= whiteToMoveNumber;

        return zobristKey;
    }

}