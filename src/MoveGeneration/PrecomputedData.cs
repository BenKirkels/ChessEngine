namespace Chess;

public static class PrecomputedData
{
    static ulong[] whitePawnAttacks = new ulong[64];
    static ulong[] blackPawnAttacks = new ulong[64];
    public static ulong pawnAttacks(int index, bool whitePiece) => whitePiece ? whitePawnAttacks[index] : blackPawnAttacks[index];
    public static ulong pawnAttacks(int index, int color) => color == Piece.WHITE ? whitePawnAttacks[index] : blackPawnAttacks[index];

    public static ulong[] knightMoves = new ulong[64];

    public static ulong[] rookMasks = new ulong[64];
    public static ulong[] bishopMasks = new ulong[64];
    public static ulong[][] rookMoves = new ulong[64][];
    public static ulong[][] bishopMoves = new ulong[64][];

    public static ulong[] kingMoves = new ulong[64];

    // N, E, S, W, NE, SE, SW, NE
    public static int[,] movesToEdge = new int[64, 8];

    public static ulong[,] xrays = new ulong[64, 8];

    public static ulong[,] alignMask = new ulong[64, 64];


    static PrecomputedData()
    {
        for (int index = 0; index < 64; index++)
        {
            // Precompute pawn attacks
            whitePawnAttacks[index] = BitBoardHelper.pawnAttacks(index, true);
            blackPawnAttacks[index] = BitBoardHelper.pawnAttacks(index, false);

            // Precompute knight moves
            knightMoves[index] = BitBoardHelper.KnightMoves(index);

            // Precompute bishop masks
            bishopMasks[index] = MagicHelper.GenerateSliderMask(index, false);

            // Precompute bishop moves
            ulong[] bishopBlockers = MagicHelper.GenerateBlockerCombinations(bishopMasks[index]);
            bishopMoves[index] = new ulong[bishopBlockers.Length];
            foreach (ulong blocker in bishopBlockers)
                bishopMoves[index][MagicNumbers.bishop[index] * blocker >> MagicNumbers.bishopShift[index]] = MagicHelper.SliderMoves(index, blocker, false);

            // Precompute rook masks
            rookMasks[index] = MagicHelper.GenerateSliderMask(index, true);

            // Precompute rook moves
            ulong[] rookBlockers = MagicHelper.GenerateBlockerCombinations(rookMasks[index]);
            rookMoves[index] = new ulong[rookBlockers.Length];
            foreach (ulong blocker in rookBlockers)
                rookMoves[index][MagicNumbers.rook[index] * blocker >> MagicNumbers.rookShift[index]] = MagicHelper.SliderMoves(index, blocker, true);

            // Precompute king moves
            kingMoves[index] = BitBoardHelper.KingMoves(index);

            int rank = BoardHelper.IndexToRank(index);
            int file = BoardHelper.IndexToFile(index);
            movesToEdge[index, 0] = 7 - rank;
            movesToEdge[index, 1] = 7 - file;
            movesToEdge[index, 2] = rank;
            movesToEdge[index, 3] = file;
            movesToEdge[index, 4] = Math.Min(7 - rank, 7 - file);
            movesToEdge[index, 5] = Math.Min(rank, 7 - file);
            movesToEdge[index, 6] = Math.Min(rank, file);
            movesToEdge[index, 7] = Math.Min(7 - rank, file);

            xrays[index, 0] = BitBoardHelper.untilEdge(index, 0);
            xrays[index, 1] = BitBoardHelper.untilEdge(index, 1);
            xrays[index, 2] = BitBoardHelper.untilEdge(index, 2);
            xrays[index, 3] = BitBoardHelper.untilEdge(index, 3);
            xrays[index, 4] = BitBoardHelper.untilEdge(index, 4);
            xrays[index, 5] = BitBoardHelper.untilEdge(index, 5);
            xrays[index, 6] = BitBoardHelper.untilEdge(index, 6);
            xrays[index, 7] = BitBoardHelper.untilEdge(index, 7);

            for (int i = 0; i < 64; i++)
            {
                alignMask[index, i] = BitBoardHelper.alignMask(index, i);
            }
        }
    }

}