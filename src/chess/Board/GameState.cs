using System.Collections.Concurrent;

namespace Chess;


/// <summary>
/// Struct <c> GameState </c> represents the state of the game.
/// It contains:
/// <para> - The castling rights. </para>
/// <para> - The en passant square. </para>
/// <para> - The zobrist key. </para>
/// </summary>
public readonly struct GameState
{
    /// <summary>
    /// The castling rights are represented by a 4-bit number.
    /// Order: white king side, white queen side, black king side, black queen side.
    /// </summary>
    public readonly int castlingRights;

    /// <summary>
    /// Stores the file of the en passant square.
    /// </summary>
    /// <remarks> -1 if there is no en passant square. </remarks>
    public readonly int enPassantFile;

    /// <summary>
    /// The zobrist key is a 64-bit number that represents the state of the game.
    /// </summary>
    public readonly ulong zobristKey;

    /// <summary>
    /// The fifty move counter is the number of half moves since the last capture or pawn move.
    /// </summary>
    /// <remarks> Will count up to 100 due to half moves. </remarks>
    public readonly int fiftyMoveCounter;


    /// <summary>
    /// Initializes a new instance of the <c> GameState </c> struct.
    /// </summary>
    /// <param name="castlingRights">4 bit short (wk wq bk bq)</param>
    /// <param name="enPassantFile">File of the en passant square</param>
    public GameState(int castlingRights, int enPassantFile, ulong zobristKey, int fiftyMoveCounter)
    {
        this.castlingRights = castlingRights;
        this.enPassantFile = enPassantFile;
        this.zobristKey = zobristKey;
        this.fiftyMoveCounter = fiftyMoveCounter;
    }

    public bool CanCastleKingSide(bool whiteToMove)
    {
        return (castlingRights & (whiteToMove ? 0b1000 : 0b10)) != 0;
    }

    public bool CanCastleQueenSide(bool whiteToMove)
    {
        return (castlingRights & (whiteToMove ? 0b100 : 0b1)) != 0;
    }
}
