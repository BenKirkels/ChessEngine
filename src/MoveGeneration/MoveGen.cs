using System.Collections;
using Chess;
using Helpers;

namespace MoveGeneration;

public class MoveGen
{
    int numberOfMoves;
    Board board;
    bool generateQuietMoves;
    bool whiteToMove;
    int pushOffset;
    int myColor;
    int myColorIndex;
    int opponentColor;
    int opponentColorIndex;
    ulong myPiecesBitboard;
    ulong opponentPiecesBitboard;
    ulong opponentDiagonalSliders;
    ulong opponentOrthogonalSliders;
    ulong myKingBitboard;
    int myKingIndex;
    ulong allPiecesBitboard;
    ulong emptySquaresBitboard;
    int enPassantFile;
    bool canCastleKingSide;
    bool canCastleQueenSide;

    public ulong attackedSquares;
    ulong pinnedPieces;
    ulong notPinnedPieces;

    //Contains all target squares that can stop the check. 
    //If not in check, all bits set to 1 
    ulong checkBlock;
    public bool inCheck;
    bool doubleCheck;


    // If generateQuietMoves is false, only captures and promotions are generated
    public void GenerateMoves(ref Span<Move> moves, Board board, bool generateQuietMoves)
    {
        this.board = board;
        this.generateQuietMoves = generateQuietMoves;

        Init();

        CalculateAttackedSquares();
        CalculatePinnedPieces();
        notPinnedPieces = ~pinnedPieces;

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
        attackedSquares = 0;
        pinnedPieces = 0;
        inCheck = false;
        doubleCheck = false;
        checkBlock = ulong.MaxValue;

        whiteToMove = board.whiteToMove;
        myColor = board.MyColor;
        myColorIndex = board.MyColorIndex;
        opponentColor = board.OpponentColor;
        opponentColorIndex = board.OpponentColorIndex;
        pushOffset = whiteToMove ? 8 : -8;

        myPiecesBitboard = board.colorBitboards[myColorIndex];
        opponentPiecesBitboard = board.colorBitboards[opponentColorIndex];

        opponentDiagonalSliders = board.diagonalSliders[opponentColorIndex];
        opponentOrthogonalSliders = board.orthogonalSliders[opponentColorIndex];

        myKingBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KING, myColor)];
        myKingIndex = BitBoardHelper.GetIndexOfLSB(myKingBitboard);

        allPiecesBitboard = board.allPiecesBitboard;
        emptySquaresBitboard = ~allPiecesBitboard;

        enPassantFile = board.gameState.enPassantFile;

        canCastleKingSide = board.CanCastleKingSide;
        canCastleQueenSide = board.CanCastleQueenSide;
    }


    // Calculate all squares attacked by opponent 
    // and check if my king is checked by a pawn or knight
    // Modify's: attackedSquares, inCheck, checkBlock 
    void CalculateAttackedSquares()
    {
        ulong pawnsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, opponentColor)];
        ulong leftAttacks = BitBoardHelper.shiftLeft(pawnsBitboard, -pushOffset - 1) & ~BitBoardHelper.fileH;
        ulong rightAttacks = BitBoardHelper.shiftLeft(pawnsBitboard, -pushOffset + 1) & ~BitBoardHelper.fileA;
        attackedSquares = leftAttacks | rightAttacks;
        if ((attackedSquares & myKingBitboard) != 0)
        {
            inCheck = true;
            checkBlock = pawnsBitboard & PrecomputedData.PawnAttacks(myKingIndex, myColor);
        }

        ulong knightsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KNIGHT, opponentColor)];
        while (knightsBitboard != 0)
        {
            int knightIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref knightsBitboard);
            ulong targetSquares = PrecomputedData.knightMoves[knightIndex];
            if ((targetSquares & myKingBitboard) != 0)
            {
                inCheck = true;
                checkBlock = BitBoardHelper.Index(knightIndex);
            }
            attackedSquares |= targetSquares;
        }

        // Prevent that the king moves to a square attacked by a slider because the king blocked that square
        ulong allPiecesBitboardWithoutMyKing = allPiecesBitboard ^ myKingBitboard;

        ulong diagonalSliders = opponentDiagonalSliders;
        while (diagonalSliders != 0)
        {
            int diagonalIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref diagonalSliders);
            attackedSquares |= PrecomputedData.BishopMoves(diagonalIndex, allPiecesBitboardWithoutMyKing);
        }

        ulong orthogonalSliders = opponentOrthogonalSliders;
        while (orthogonalSliders != 0)
        {
            int orthogonalIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref orthogonalSliders);
            attackedSquares |= PrecomputedData.RookMoves(orthogonalIndex, allPiecesBitboardWithoutMyKing);
        }

        ulong opponentKingBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KING, opponentColor)];
        int opponentKingIndex = BitBoardHelper.GetIndexOfLSB(opponentKingBitboard);
        attackedSquares |= PrecomputedData.kingMoves[opponentKingIndex];
    }

    // Calculate all pinned pieces 
    // and check if my king is checked by a slider
    // Modify's: pinnedPieces, inCheck, doubleCheck, checkBlock
    public void CalculatePinnedPieces()
    {
        int startdir = 0;
        int enddir = 7;

        if (opponentOrthogonalSliders == 0) startdir = 4;
        if (opponentDiagonalSliders == 0) enddir = 3;

        // Perform xray in all directions from the king
        for (int dir = startdir; dir <= enddir; dir++)
        {
            ulong xray = PrecomputedData.xrays[myKingIndex, dir];
            bool diagonal = dir > 3 ? true : false;

            ulong sliderPieces = diagonal ? opponentDiagonalSliders : opponentOrthogonalSliders;

            if ((xray & sliderPieces) == 0) continue; // No slider in this direction

            ulong pinnedMask = 0;

            // Step over xray
            int stepOfset = BoardHelper.directions[dir];
            bool friendlyPieceAlongTheWay = false;
            for (int dist = 1; dist <= PrecomputedData.movesToEdge[myKingIndex, dir]; dist++)
            {
                int targetIndex = myKingIndex + stepOfset * dist;

                BitBoardHelper.SetIndex(ref pinnedMask, targetIndex);

                if (!board.IsPieceAtSquare(targetIndex)) continue; // Ignore empty squares

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
        ulong pawnsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.PAWN, myColor)];

        ulong singlePush = BitBoardHelper.shiftLeft(pawnsBitboard, pushOffset) & emptySquaresBitboard;
        ulong doublePush = BitBoardHelper.shiftLeft(singlePush, pushOffset) & emptySquaresBitboard & (whiteToMove ? BitBoardHelper.rank4 : BitBoardHelper.rank5);
        singlePush &= checkBlock;
        doublePush &= checkBlock;
        ulong singlePushNoPromotion = singlePush & BitBoardHelper.NOT_PROMOTION_RANKS;
        ulong singlePushPromotion = singlePush & BitBoardHelper.PROMOTION_RANKS;

        ulong capturesLeft = BitBoardHelper.shiftLeft(pawnsBitboard, pushOffset - 1) & ~BitBoardHelper.fileH & opponentPiecesBitboard;
        capturesLeft &= checkBlock;
        ulong capturesLeftNoPromotion = capturesLeft & BitBoardHelper.NOT_PROMOTION_RANKS;
        ulong capturesLeftPromotion = capturesLeft & BitBoardHelper.PROMOTION_RANKS;

        ulong capturesRight = BitBoardHelper.shiftLeft(pawnsBitboard, pushOffset + 1) & ~BitBoardHelper.fileA & opponentPiecesBitboard;
        capturesRight &= checkBlock;
        ulong capturesRightNoPromotion = capturesRight & BitBoardHelper.NOT_PROMOTION_RANKS;
        ulong capturesRightPromotion = capturesRight & BitBoardHelper.PROMOTION_RANKS;

        // Single push / double push
        if (generateQuietMoves)
        {
            while (singlePushNoPromotion != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref singlePushNoPromotion);
                int startIndex = targetIndex - pushOffset;
                // Add move if pawn not pinned or if move is along the pin direction
                if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex) || (PrecomputedData.alignMask[myKingIndex, startIndex] == PrecomputedData.alignMask[myKingIndex, targetIndex]))
                    moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.QUIET_MOVE);
            }
            while (doublePush != 0)
            {
                int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref doublePush);
                int startIndex = targetIndex - 2 * pushOffset;
                // Add move if pawn not pinned or if move is along the pin direction
                if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex) || (PrecomputedData.alignMask[myKingIndex, startIndex] == PrecomputedData.alignMask[myKingIndex, targetIndex]))
                    moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.DOUBLE_PAWN_PUSH);
            }
        }

        // Promotions
        while (singlePushPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref singlePushPromotion);
            int startIndex = targetIndex - pushOffset;
            // Add move if pawn not pinned
            if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex))
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
            if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex))
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
            if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex))
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
            if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex) || (PrecomputedData.alignMask[myKingIndex, startIndex] == PrecomputedData.alignMask[myKingIndex, targetIndex]))
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.CAPTURE);
        }
        while (capturesRightNoPromotion != 0)
        {
            int targetIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref capturesRightNoPromotion);
            int startIndex = targetIndex - pushOffset - 1;
            // Add move if pawn not pinned or if move is along the pin direction
            if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex) || (PrecomputedData.alignMask[myKingIndex, startIndex] == PrecomputedData.alignMask[myKingIndex, targetIndex]))
                moves[numberOfMoves++] = new Move(startIndex, targetIndex, Move.CAPTURE);
        }

        // En passant
        if (enPassantFile != -1)
        {
            int enPassantRank = whiteToMove ? 5 : 2;
            int moveToIndex = BoardHelper.FileRankToIndex(enPassantFile, enPassantRank);
            int capturedPawnIndex = moveToIndex - pushOffset;
            ulong pawnsThatCanCaptureEnPassent = pawnsBitboard & PrecomputedData.PawnAttacks(moveToIndex, opponentColor);
            while (pawnsThatCanCaptureEnPassent != 0)
            {
                int startIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref pawnsThatCanCaptureEnPassent);
                // Add move if pawn not pinned or if move is along the pin direction
                if (BitBoardHelper.IsBitSet(notPinnedPieces, startIndex) || (PrecomputedData.alignMask[myKingIndex, startIndex] == PrecomputedData.alignMask[myKingIndex, moveToIndex]))
                    // Check if en passent does not result in check
                    if (!InCheckAfterEnPassent(startIndex, moveToIndex, capturedPawnIndex))
                        moves[numberOfMoves++] = new Move(startIndex, moveToIndex, Move.EN_PASSANT);
            }
        }
    }
    bool InCheckAfterEnPassent(int startIndex, int targetIndex, int enPassantIndex)
    {
        if (opponentOrthogonalSliders != 0)
        {
            ulong maskerBlockers = allPiecesBitboard ^ (BitBoardHelper.Index(startIndex) | BitBoardHelper.Index(targetIndex) | BitBoardHelper.Index(enPassantIndex));
            ulong rookAttacks = PrecomputedData.RookMoves(myKingIndex, maskerBlockers);
            return (rookAttacks & opponentOrthogonalSliders) != 0;
        }
        return false;
    }
    void KnightMoves(ref Span<Move> moves)
    {
        ulong knightsBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KNIGHT, myColor)];

        knightsBitboard &= notPinnedPieces; // Remove pinned knights

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
            bishopsBitboard &= notPinnedPieces;
        }
        while (bishopsBitboard != 0)
        {
            int bishopIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref bishopsBitboard);
            ulong bishopMoves = PrecomputedData.BishopMoves(bishopIndex, allPiecesBitboard) & ~myPiecesBitboard;

            bishopMoves &= checkBlock; // Remove squares that can't block the check

            if (BitBoardHelper.IsBitSet(pinnedPieces, bishopIndex))
            {
                // Pinned bishop can only move along the pin direction
                ulong pinDirection = PrecomputedData.alignMask[myKingIndex, bishopIndex];
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
            rooksBitboard &= notPinnedPieces;
        }
        while (rooksBitboard != 0)
        {
            int rookIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref rooksBitboard);
            ulong rookMoves = PrecomputedData.RookMoves(rookIndex, allPiecesBitboard) & ~myPiecesBitboard;

            rookMoves &= checkBlock; // Remove squares that can't block the check

            if (BitBoardHelper.IsBitSet(pinnedPieces, rookIndex))
            {
                // Pinned rook can only move along the pin direction
                ulong pinDirection = PrecomputedData.alignMask[myKingIndex, rookIndex];
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
            queensBitboard &= notPinnedPieces;
        }
        while (queensBitboard != 0)
        {
            int queenIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref queensBitboard);
            ulong queenMoves = (PrecomputedData.RookMoves(queenIndex, allPiecesBitboard) | PrecomputedData.BishopMoves(queenIndex, allPiecesBitboard)) & ~myPiecesBitboard;
            queenMoves &= checkBlock; // Remove squares that can't block the check

            if (BitBoardHelper.IsBitSet(pinnedPieces, queenIndex))
            {
                // Pinned queen can only move along the pin direction
                ulong pinDirection = PrecomputedData.alignMask[myKingIndex, queenIndex];
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
        ulong kingBitboard = board.pieceBitboards[Piece.MakePiece(Piece.KING, myColor)];
        int kingIndex = BitBoardHelper.ClearAndGetIndexOfLSB(ref kingBitboard);
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
                ulong kingSideSquares = whiteToMove ? 0x60UL : 0x6000000000000000UL;
                if ((blockedSquares & kingSideSquares) == 0)
                    moves[numberOfMoves++] = new Move(kingIndex, kingIndex + 2, Move.KING_CASTLE);
            }
            if (canCastleQueenSide)
            {
                ulong queenSideSquares = whiteToMove ? 0xCUL : 0xC00000000000000UL;
                ulong rookSideSquare = whiteToMove ? 0x2UL : 0x200000000000000UL;
                if ((blockedSquares & queenSideSquares) == 0 && (rookSideSquare & allPiecesBitboard) == 0)
                    moves[numberOfMoves++] = new Move(kingIndex, kingIndex - 2, Move.QUEEN_CASTLE);
            }
        }
    }
}