using System.ComponentModel;
using System.Reflection.Metadata;

namespace Chess;

public class MoveGen
{
    int numberOfMoves;
    Board board;
    bool generateQuietMoves;
    bool whiteToMove;
    int myColor;
    int myColorIndex;
    int opponentColor;
    int opponentColorIndex;
    ulong myPiecesBitboard;
    ulong opponentPiecesBitboard;
    ulong opponentDiagonalSlider;
    ulong opponentOrthogonalSlider;
    int friendlyKingIndex;
    ulong allPiecesBitboard;
    int enPassantFile;
    bool canCastleKingSide;
    bool canCastleQueenSide;

    public ulong attackedSquares;
    ulong pinnedPieces;

    //Contains all target squares that can stop the check. 
    //If not in check, all bits set to 1 
    ulong checkBlock = ulong.MaxValue;
    public bool inCheck;
    bool doubleCheck;


    // If generateQuietMoves is false, only captures and promotions are generated
    public void GenerateMoves(ref Span<Move> moves, Board board, bool generateQuietMoves)
    {
        this.board = board;
        this.generateQuietMoves = generateQuietMoves;

        Init();

        KingMoves(ref moves);

        if (!doubleCheck)
        {
            PawnMoves(ref moves);
            KnightMoves(ref moves);
            BishopMoves(ref moves);
            RookMoves(ref moves);
            QueenMoves(ref moves);
        }
        moves = moves[..numberOfMoves];
    }

    void Init()
    {
        numberOfMoves = 0;

        whiteToMove = board.whiteToMove;
        myColor = board.MyColor;
        myColorIndex = board.MyColorIndex;
        opponentColor = board.OpponentColor;
        opponentColorIndex = board.OpponentColorIndex;

        myPiecesBitboard = board.colorBitboards[myColorIndex];
        opponentPiecesBitboard = board.colorBitboards[opponentColorIndex];

        opponentDiagonalSlider = board.pieceBitboards[Piece.MakePiece(Piece.BISHOP, opponentColor)] | board.pieceBitboards[Piece.MakePiece(Piece.QUEEN, opponentColor)];
        opponentOrthogonalSlider = board.pieceBitboards[Piece.MakePiece(Piece.ROOK, opponentColor)] | board.pieceBitboards[Piece.MakePiece(Piece.QUEEN, opponentColor)];

        ulong friendlyKingBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KING, myColor)];
        friendlyKingIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref friendlyKingBitboard);

        allPiecesBitboard = board.allPiecesBitboard;

        enPassantFile = board.gameState.enPassantFile;

        canCastleKingSide = board.CanCastleKingSide;
        canCastleQueenSide = board.CanCastleQueenSide;

        AnalyseEnemyAttacks();
    }

    void AnalyseEnemyAttacks()
    {
        bool hasQueen = false, hasRook = false, hasBishop = false;

        ulong pawnsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, opponentColor)];
        while (pawnsBitboard != 0)
        {
            int pawnIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref pawnsBitboard);
            ulong targetSquares = PrecomputedData.pawnAttacks(pawnIndex, opponentColor);
            if (BitBoardHelper.IsBitSet(targetSquares, friendlyKingIndex))
            {
                inCheck = true;
                checkBlock = BitBoardHelper.Index(pawnIndex);
            }
            attackedSquares |= targetSquares;
        }

        ulong knightsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KNIGHT, opponentColor)];
        while (knightsBitboard != 0)
        {
            int knightIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref knightsBitboard);
            ulong targetSquares = PrecomputedData.knightMoves[knightIndex];
            if (BitBoardHelper.IsBitSet(targetSquares, friendlyKingIndex))
            {
                inCheck = true;
                checkBlock = BitBoardHelper.Index(knightIndex);
            }
            attackedSquares |= targetSquares;
        }

        ulong bishopsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.BISHOP, opponentColor)];
        while (bishopsBitboard != 0)
        {
            hasBishop = true;
            int bishopIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref bishopsBitboard);
            attackedSquares |= PrecomputedData.bishopMoves[bishopIndex][(allPiecesBitboard & PrecomputedData.bishopMasks[bishopIndex]) * MagicNumbers.bishop[bishopIndex] >> MagicNumbers.bishopShift[bishopIndex]];
        }

        ulong rooksBitboard = board.pieceBitboards[Piece.MakePiece(Piece.ROOK, opponentColor)];
        while (rooksBitboard != 0)
        {
            hasRook = true;
            int rookIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref rooksBitboard);
            attackedSquares |= PrecomputedData.rookMoves[rookIndex][(allPiecesBitboard & PrecomputedData.rookMasks[rookIndex]) * MagicNumbers.rook[rookIndex] >> MagicNumbers.rookShift[rookIndex]];
        }

        ulong queensBitboard = board.pieceBitboards[Piece.MakePiece(Piece.QUEEN, opponentColor)];
        while (queensBitboard != 0)
        {
            hasQueen = true;
            int queenIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref queensBitboard);
            attackedSquares |= (PrecomputedData.rookMoves[queenIndex][(allPiecesBitboard & PrecomputedData.rookMasks[queenIndex]) * MagicNumbers.rook[queenIndex] >> MagicNumbers.rookShift[queenIndex]] |
                                PrecomputedData.bishopMoves[queenIndex][(allPiecesBitboard & PrecomputedData.bishopMasks[queenIndex]) * MagicNumbers.bishop[queenIndex] >> MagicNumbers.bishopShift[queenIndex]]);
        }

        ulong opponentKingBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KING, opponentColor)];
        int opponentKingIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref opponentKingBitboard);
        attackedSquares |= PrecomputedData.kingMoves[opponentKingIndex];

        // Pinned pieces
        int startdir = 0;
        int enddir = 7;
        if (!hasQueen)
        {
            if (!hasRook) startdir = 4;
            if (!hasBishop) enddir = 3;
        }

        // Perform xray in all directions from the king
        for (int dir = startdir; dir <= enddir; dir++)
        {
            ulong xray = PrecomputedData.xrays[friendlyKingIndex, dir];
            bool diagonal = dir > 3 ? true : false;

            ulong sliderPieces = diagonal ? opponentDiagonalSlider : opponentOrthogonalSlider;
            ulong pinnedMask = 0;

            if ((xray & sliderPieces) == 0) continue; // No slider in this direction

            // Step over xray
            bool friendlyPieceAlongTheWay = false;
            for (int dist = 1; dist <= PrecomputedData.movesToEdge[friendlyKingIndex, dir]; dist++)
            {
                int targetIndex = friendlyKingIndex + dir * dist;

                BitBoardHelper.SetIndex(ref pinnedMask, targetIndex);

                if (!board.PieceAtSquare(targetIndex)) continue; // Ignore empty squares

                // Square is occupied

                if (BitBoardHelper.IsBitSet(myPiecesBitboard, targetIndex))
                {
                    // My piece 
                    if (friendlyPieceAlongTheWay) break; // Second piece so pin is impossible
                    friendlyPieceAlongTheWay = true;
                    continue; // Continue looking for pin
                }

                // Opponent piece
                if (BitBoardHelper.IsBitSet(sliderPieces, targetIndex))
                {
                    // Opponent piece can pin

                    if (friendlyPieceAlongTheWay)
                        // Friendly piece is pinned
                        pinnedPieces |= pinnedMask;
                    else
                    {
                        // No friendly piece so check
                        doubleCheck = inCheck;
                        inCheck = true;
                        checkBlock = pinnedMask;
                    }
                }
                break; // Stop looking when opponent piece is found
            }
        }
    }


    void PawnMoves(ref Span<Move> moves)
    {
        int pushOffset = whiteToMove ? 8 : -8;

        ulong pawnsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, myColor)];
        ulong singlePush = (pawnsBitboard << pushOffset) & ~allPiecesBitboard;
        ulong singlePushNoPromotion = singlePush & ~BitBoardHelper.rank1 & ~BitBoardHelper.rank8;
        ulong singlePushPromotion = singlePush & (BitBoardHelper.rank1 | BitBoardHelper.rank8);
        ulong doublePush = (singlePush << pushOffset) & ~allPiecesBitboard & (whiteToMove ? BitBoardHelper.rank4 : BitBoardHelper.rank5);

        ulong capturesLeft = (pawnsBitboard << (pushOffset - 1)) & ~BitBoardHelper.fileH & opponentPiecesBitboard;
        ulong capturesLeftNoPromotion = capturesLeft & ~BitBoardHelper.rank1 & ~BitBoardHelper.rank8;
        ulong capturesLeftPromotion = capturesLeft & (BitBoardHelper.rank1 | BitBoardHelper.rank8);

        ulong capturesRight = (pawnsBitboard << (pushOffset + 1)) & ~BitBoardHelper.fileA & opponentPiecesBitboard;
        ulong capturesRightNoPromotion = capturesRight & ~BitBoardHelper.rank1 & ~BitBoardHelper.rank8;
        ulong capturesRightPromotion = capturesRight & (BitBoardHelper.rank1 | BitBoardHelper.rank8);

        // Single push / double push
        if (generateQuietMoves)
        {
            while (singlePushNoPromotion != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref singlePushNoPromotion);
                int startIndex = targetIndex - pushOffset;
                // Add move if pawn not pinned or if move is along the pin direction
                if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex) || (PrecomputedData.alignMask[friendlyKingIndex, startIndex] == BitBoardHelper.Index(targetIndex)))
                    moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.QUIET_MOVE);
            }
            while (doublePush != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref doublePush);
                int startIndex = targetIndex - 2 * pushOffset;
                // Add move if pawn not pinned or if move is along the pin direction
                if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex) || (PrecomputedData.alignMask[friendlyKingIndex, startIndex] == BitBoardHelper.Index(targetIndex)))
                    moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.DOUBLE_PAWN_PUSH);
            }
        }

        // Promotions
        while (singlePushPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref singlePushPromotion);
            int startIndex = targetIndex - pushOffset;
            // Add move if pawn not pinned
            if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex))
            {
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.QUEEN_PROMOTION);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.ROOK_PROMOTION);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.BISHOP_PROMOTION);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.KNIGHT_PROMOTION);
            }
        }
        // Promotions with capture
        while (capturesLeftPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref capturesLeftPromotion);
            int startIndex = targetIndex - pushOffset + 1;
            // Add move if pawn not pinned
            if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex))
            {
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.QUEEN_PROMOTION_CAPTURE);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.ROOK_PROMOTION_CAPTURE);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.BISHOP_PROMOTION_CAPTURE);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.KNIGHT_PROMOTION_CAPTURE);
            }
        }
        while (capturesRightPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref capturesRightPromotion);
            int startIndex = targetIndex - pushOffset - 1;
            // Add move if pawn not pinned
            if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex))
            {
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.QUEEN_PROMOTION_CAPTURE);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.ROOK_PROMOTION_CAPTURE);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.BISHOP_PROMOTION_CAPTURE);
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.KNIGHT_PROMOTION_CAPTURE);
            }
        }
        // Captures
        while (capturesLeftNoPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref capturesLeftNoPromotion);
            int startIndex = targetIndex - pushOffset + 1;
            // Add move if pawn not pinned or if move is along the pin direction
            if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex) || (PrecomputedData.alignMask[friendlyKingIndex, startIndex] == BitBoardHelper.Index(targetIndex)))
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.CAPTURE);
        }
        while (capturesRightNoPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref capturesRightNoPromotion);
            int startIndex = targetIndex - pushOffset - 1;
            // Add move if pawn not pinned or if move is along the pin direction
            if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex) || (PrecomputedData.alignMask[friendlyKingIndex, startIndex] == BitBoardHelper.Index(targetIndex)))
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.CAPTURE);
        }

        // En passant
        if (enPassantFile != -1)
        {
            int enPassantRank = whiteToMove ? 5 : 2;
            int enPassantIndex = BoardHelper.FileRankToIndex(enPassantFile, enPassantRank);
            ulong pawnsThatCanCaptureEnPassent = pawnsBitboard & PrecomputedData.pawnAttacks(enPassantIndex, opponentColor);
            while (pawnsThatCanCaptureEnPassent != 0)
            {
                int startIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref pawnsThatCanCaptureEnPassent);
                // Add move if pawn not pinned or if move is along the pin direction
                if (!BitBoardHelper.IsBitSet(pinnedPieces, startIndex) || (PrecomputedData.alignMask[friendlyKingIndex, startIndex] == BitBoardHelper.Index(enPassantIndex)))
                    // Check if en passent does not result in check
                    if (!InCheckAfterEnPassent(startIndex, enPassantIndex, enPassantIndex - pushOffset))
                        moves[numberOfMoves++] = new Move(startIndex, enPassantIndex, Move.EN_PASSANT);
            }
        }
    }
    bool InCheckAfterEnPassent(int startIndex, int targetIndex, int enPassantIndex)
    {
        if (opponentOrthogonalSlider != 0)
        {
            ulong maskerBlockers = allPiecesBitboard ^ (BitBoardHelper.Index(startIndex) | BitBoardHelper.Index(targetIndex) | BitBoardHelper.Index(enPassantIndex));
            ulong rookAttacks = PrecomputedData.rookMoves[friendlyKingIndex][(maskerBlockers & PrecomputedData.rookMasks[friendlyKingIndex]) * MagicNumbers.rook[friendlyKingIndex] >> MagicNumbers.rookShift[friendlyKingIndex]];
            return (rookAttacks & opponentOrthogonalSlider) != 0;
        }
        return false;
    }
    void KnightMoves(ref Span<Move> moves)
    {
        ulong knightsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KNIGHT, myColor)];

        knightsBitboard &= ~pinnedPieces; // Remove pinned knights

        while (knightsBitboard != 0)
        {
            int knightIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref knightsBitboard);
            ulong knightMoves = PrecomputedData.knightMoves[knightIndex] & ~myPiecesBitboard;
            knightMoves &= checkBlock; // Remove squares that can't block the check
            while (knightMoves != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref knightMoves);
                if (BitBoardHelper.IsBitSet(opponentPiecesBitboard, targetIndex))
                    moves[numberOfMoves++] = new Move(knightIndex, targetIndex, Move.CAPTURE);
                else if (generateQuietMoves)
                    moves[numberOfMoves++] = new Move(knightIndex, targetIndex, Move.QUIET_MOVE);
            }
        }
    }

    void BishopMoves(ref Span<Move> moves)
    {
        ulong bishopsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.BISHOP, myColor)];
        if (inCheck)
        {
            // When in check, pinned bishops can not move
            bishopsBitboard &= ~pinnedPieces;
        }
        while (bishopsBitboard != 0)
        {
            int bishopIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref bishopsBitboard);
            ulong bishopMoves = PrecomputedData.bishopMoves[bishopIndex][(allPiecesBitboard & PrecomputedData.bishopMasks[bishopIndex]) * MagicNumbers.bishop[bishopIndex] >> MagicNumbers.bishopShift[bishopIndex]] & ~myPiecesBitboard;

            bishopMoves &= checkBlock; // Remove squares that can't block the check

            if (BitBoardHelper.IsBitSet(pinnedPieces, bishopIndex))
            {
                // Pinned bishop can only move along the pin direction
                ulong pinDirection = PrecomputedData.alignMask[friendlyKingIndex, bishopIndex];
                bishopMoves &= pinDirection;
            }

            while (bishopMoves != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref bishopMoves);
                if (BitBoardHelper.IsBitSet(opponentPiecesBitboard, targetIndex))
                    moves[numberOfMoves++] = new Move(bishopIndex, targetIndex, Move.CAPTURE);
                else if (generateQuietMoves)
                    moves[numberOfMoves++] = new Move(bishopIndex, targetIndex, Move.QUIET_MOVE);
            }
        }
    }

    void RookMoves(ref Span<Move> moves)
    {
        ulong rooksBitboard = board.pieceBitboards[Piece.MakePiece(Piece.ROOK, myColor)];
        if (inCheck)
        {
            // When in check, pinned rooks can not move
            rooksBitboard &= ~pinnedPieces;
        }
        while (rooksBitboard != 0)
        {
            int rookIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref rooksBitboard);
            ulong rookMoves = PrecomputedData.rookMoves[rookIndex][(allPiecesBitboard & PrecomputedData.rookMasks[rookIndex]) * MagicNumbers.rook[rookIndex] >> MagicNumbers.rookShift[rookIndex]] & ~myPiecesBitboard;

            rookMoves &= checkBlock; // Remove squares that can't block the check

            if (BitBoardHelper.IsBitSet(pinnedPieces, rookIndex))
            {
                // Pinned rook can only move along the pin direction
                ulong pinDirection = PrecomputedData.alignMask[friendlyKingIndex, rookIndex];
                rookMoves &= pinDirection;
            }

            while (rookMoves != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref rookMoves);
                if (BitBoardHelper.IsBitSet(opponentPiecesBitboard, targetIndex))
                    moves[numberOfMoves++] = new Move(rookIndex, targetIndex, Move.CAPTURE);
                else if (generateQuietMoves)
                    moves[numberOfMoves++] = new Move(rookIndex, targetIndex, Move.QUIET_MOVE);
            }
        }
    }

    void QueenMoves(ref Span<Move> moves)
    {
        ulong queensBitboard = board.pieceBitboards[Piece.MakePiece(Piece.QUEEN, myColor)];
        if (inCheck)
        {
            // When in check, pinned rooks can not move
            queensBitboard &= ~pinnedPieces;
        }
        while (queensBitboard != 0)
        {
            int queenIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref queensBitboard);
            ulong queenMoves = (PrecomputedData.rookMoves[queenIndex][(allPiecesBitboard & PrecomputedData.rookMasks[queenIndex]) * MagicNumbers.rook[queenIndex] >> MagicNumbers.rookShift[queenIndex]] |
                                PrecomputedData.bishopMoves[queenIndex][(allPiecesBitboard & PrecomputedData.bishopMasks[queenIndex]) * MagicNumbers.bishop[queenIndex] >> MagicNumbers.bishopShift[queenIndex]]) & ~myPiecesBitboard;

            queenMoves &= checkBlock; // Remove squares that can't block the check

            if (BitBoardHelper.IsBitSet(pinnedPieces, queenIndex))
            {
                // Pinned queen can only move along the pin direction
                ulong pinDirection = PrecomputedData.alignMask[friendlyKingIndex, queenIndex];
                queenMoves &= pinDirection;
            }

            while (queenMoves != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref queenMoves);
                if (BitBoardHelper.IsBitSet(opponentPiecesBitboard, targetIndex))
                    moves[numberOfMoves++] = new Move(queenIndex, targetIndex, Move.CAPTURE);
                else if (generateQuietMoves)
                    moves[numberOfMoves++] = new Move(queenIndex, targetIndex, Move.QUIET_MOVE);
            }
        }
    }

    void KingMoves(ref Span<Move> moves)
    {
        int kingIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref board.pieceBitboards[Piece.MakePiece(Piece.KING, myColor)]);
        ulong kingMoves = PrecomputedData.kingMoves[kingIndex] & ~myPiecesBitboard;

        kingMoves &= ~attackedSquares; // Remove squares attacked by opponent

        while (kingMoves != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref kingMoves);
            if (BitBoardHelper.IsBitSet(opponentPiecesBitboard, targetIndex))
                moves[numberOfMoves++] = new Move(kingIndex, targetIndex, Move.CAPTURE);
            else if (generateQuietMoves)
                moves[numberOfMoves++] = new Move(kingIndex, targetIndex, Move.QUIET_MOVE);
        }

        // Castling
        if (generateQuietMoves && !inCheck)
        {
            ulong blockedSquares = attackedSquares | allPiecesBitboard;
            if (canCastleKingSide)
            {
                ulong kingSideSquares = whiteToMove ? (ulong)0x6 : 0x600000000000000;
                if ((blockedSquares & kingSideSquares) == 0)
                    moves[numberOfMoves++] = new Move(kingIndex, kingIndex + 2, Move.KING_CASTLE);
            }
            if (canCastleQueenSide)
            {
                ulong queenSideSquares = whiteToMove ? (ulong)0x30 : 0x3000000000000000;
                if ((blockedSquares & queenSideSquares) == 0)
                    moves[numberOfMoves++] = new Move(kingIndex, kingIndex - 2, Move.QUEEN_CASTLE);
            }
        }
    }
}