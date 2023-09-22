using Helpers;

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
    private readonly static ulong[,] piecesNumbers = new ulong[Piece.MAX_PIECE_NUMBER + 1, 64];

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
    private readonly static ulong whiteToMoveNumber;

    /// <summary>
    /// Generates the random numbers used to generate the zobrist key.
    /// </summary>
    static Zobrist()
    {
        Random random = new Random(29426028);

        // Pieces
        foreach (int i in Piece.PieceNumbers)
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

    public static ulong GeneratePiecesZobristKey(ulong[] pieceBitboards, bool whiteToMove)
    {
        ulong zobristKey = 0;

        // Pieces
        foreach (int i in Piece.PieceNumbers)
        {
            ulong pieceBitBoard = pieceBitboards[i];
            while (pieceBitBoard != 0)
            {
                int index = BitBoardHelper.ClearAndGetIndexOfLSB(ref pieceBitBoard);
                zobristKey ^= piecesNumbers[i, index];
            }
        }
        // Side to move
        if (whiteToMove)
            zobristKey ^= whiteToMoveNumber;

        return zobristKey;
    }

    public static void UpdatePiecesZobristKey(ref ulong zobristKey, int piece, int square)
    {
        zobristKey ^= piecesNumbers[piece, square];
    }
    public static ulong GenerateGameStateZobristKey(int castlingRights, int enPassantFile)
    {
        ulong zobristKey = 0;

        // Castling rights
        zobristKey ^= castlingRightsNumbers[castlingRights];

        // En passant
        if (enPassantFile != -1)
            zobristKey ^= enPassantNumbers[enPassantFile];

        return zobristKey;
    }

    public static void SwitchColor(ref ulong zobristKey)
    {
        zobristKey ^= whiteToMoveNumber;
    }
    public static ulong GenerateZobristKey(ulong[] pieceBitBoards, int castlingRights, int enPassantSquare, bool whiteToMove)
    {
        ulong zobristKey = GeneratePiecesZobristKey(pieceBitBoards, whiteToMove);

        zobristKey ^= GenerateGameStateZobristKey(castlingRights, BoardHelper.IndexToFile(enPassantSquare));

        return zobristKey;
    }

}