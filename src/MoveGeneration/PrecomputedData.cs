namespace Chess;

public static class PrecomputedData
{
    static ulong[] whitePawnAttacks = new ulong[64];
    static ulong[] blackPawnAttacks = new ulong[64];
    static ulong[] knightMoves = new ulong[64];

    static ulong[] rookMasks = new ulong[64];
    static ulong[] bishopMasks = new ulong[64];
    static ulong[][] rookMoves = new ulong[64][];
    static ulong[][] bishopMoves = new ulong[64][];

    public static ulong pawnAttacks(int index, bool whitePiece) => whitePiece ? whitePawnAttacks[index] : blackPawnAttacks[index];
    public static ulong pawnAttacks(int index, int color) => color == Piece.WHITE ? whitePawnAttacks[index] : blackPawnAttacks[index];

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
            foreach (ulong blocker in bishopBlockers)
                bishopMoves[index][MagicNumbers.bishop[index] * blocker >> MagicNumbers.bishopShift[index]] = MagicHelper.SliderMoves(index, blocker, false);

            // Precompute rook masks
            rookMasks[index] = MagicHelper.GenerateSliderMask(index, true);

            // Precompute rook moves
            ulong[] rookBlockers = MagicHelper.GenerateBlockerCombinations(rookMasks[index]);
            foreach (ulong blocker in rookBlockers)
                rookMoves[index][MagicNumbers.rook[index] * blocker >> MagicNumbers.rookShift[index]] = MagicHelper.SliderMoves(index, blocker, true);
        }
    }

}