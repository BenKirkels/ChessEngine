using System;

namespace Chess;

public static class FenHelper
{
    /// <summary>
    /// Generates the bitboards according to the FEN string.
    /// </summary>
    /// <param name="piecePlacement"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ulong[] BitBoards(string piecePlacement)
    {
        string[] ranks = piecePlacement.Split('/');
        if (ranks.Length != 8)
            throw new ArgumentException("Invalid FEN string: Piece placement is incomplete");

        ulong[] bitboards = new ulong[Piece.MAX_PIECE_NUMBER];
        for (int rank = 0, i = 7; rank < 8; rank++, i--)
        {
            int file = 0;
            foreach (char c in ranks[i])
            {
                if (c >= '1' && c <= '8')
                {
                    file += c - '0';
                }
                else
                {
                    int piece = char.ToLower(c) switch
                    {
                        'p' => Piece.PAWN,
                        'n' => Piece.KNIGHT,
                        'b' => Piece.BISHOP,
                        'r' => Piece.ROOK,
                        'q' => Piece.QUEEN,
                        'k' => Piece.KING,
                        _ => throw new ArgumentException("Invalid FEN string: Invalid character in piece placement"),
                    };
                    // ASCII code for a-z is 97-122, for A-Z is 65-90
                    // If the character is lowercase (>= 'a'), the piece is black, otherwise it is white
                    bitboards[Piece.MakePiece(piece, c < 'a')] |= 1UL << (rank * 8 + file);
                    file++;
                }
            }
            if (file != 8)
                throw new ArgumentException("Invalid FEN string: Rank is incomplete");
        }
        return bitboards;
    }

    /// <summary>
    /// Generates the castling rights according to the FEN string.
    /// </summary>
    /// <returns> 4 bit int (wk wq bk bq) </returns>
    public static int CastlingRights(string castlingRights)
    {
        // Order: white king side, white queen side, black king side, black queen side.
        int rights = 0;
        foreach (char c in castlingRights)
        {
            // Upper case is white, lower case is black
            switch (c)
            {
                case 'K':
                    rights |= 8;
                    break;
                case 'Q':
                    rights |= 4;
                    break;
                case 'k':
                    rights |= 2;
                    break;
                case 'q':
                    rights |= 1;
                    break;
                case '-':
                    if (castlingRights.Length != 1)
                        throw new ArgumentException("Invalid FEN string: Conflicting castling rights");
                    break;
                default:
                    throw new ArgumentException("Invalid FEN string: Invalid character in castling rights");
            }
        }
        return rights;
    }

    /// <summary>
    /// Generates the en passant square according to the FEN string.
    /// </summary>
    /// <returns> <para> index of the en passent square </para>
    ///           <para> -1 if there is no en passant square </para>
    /// </returns>
    public static int EnPassantSquare(string enPassantSquare)
    {
        if (enPassantSquare == "-")
            return -1;
        if (enPassantSquare.Length != 2)
            throw new ArgumentException("Invalid FEN string: Invalid en passant square");
        int file = enPassantSquare[0] - 'a';
        int rank = enPassantSquare[1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            throw new ArgumentException("Invalid FEN string: Invalid en passant square");
        return rank * 8 + file;
    }
}
