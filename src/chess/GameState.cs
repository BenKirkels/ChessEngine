namespace Chess
{

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
        /// The en passant square is represented by its index.
        /// </summary>
        /// <remarks> -1 if there is no en passant square. </remarks>
        public readonly int enPassantSquare;

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
        /// <param name="enPassantSquare">Index of the en passant square</param>
        public GameState(int castlingRights, int enPassantSquare, ulong zobristKey, int fiftyMoveCounter)
        {
            this.castlingRights = castlingRights;
            this.enPassantSquare = enPassantSquare;
            this.zobristKey = zobristKey;
            this.fiftyMoveCounter = fiftyMoveCounter;
        }
    }
}