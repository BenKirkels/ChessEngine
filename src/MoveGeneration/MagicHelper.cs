using System.Numerics;
using Chess;
using Helpers;

namespace MoveGeneration;

public static class MagicHelper
{
    // used for generating rook and bishop masks
    public static ulong GenerateSliderMask(int index, bool rookMoves)
    {
        ulong mask = 0;
        Square[] directions = rookMoves ? Square.rookDirections : Square.bishopDirections;

        Square startSquare = new Square(index);

        foreach (Square dir in directions)
            for (int dist = 1; dist < 8; dist++)
            {
                Square targetSquare = startSquare + dir * dist;
                // use nextSquare to ignore the last square at the edge of the board
                Square nextSquare = startSquare + dir * (dist + 1);

                if (nextSquare.IsValid)
                    BitBoardHelper.SetIndex(ref mask, targetSquare.index);
                else
                    break;
            }
        return mask;
    }

    public static ulong SliderMoves(int index, ulong blockers, bool rookMoves)
    {
        ulong moves = 0;
        Square[] directions = rookMoves ? Square.rookDirections : Square.bishopDirections;

        Square startSquare = new Square(index);

        foreach (Square dir in directions)
            for (int dist = 1; dist < 8; dist++)
            {
                Square targetSquare = startSquare + dir * dist;
                if (targetSquare.IsValid)
                {
                    BitBoardHelper.SetIndex(ref moves, targetSquare.index);
                    if (BitBoardHelper.IsBitSet(blockers, targetSquare.index))
                        break;
                }
                else
                    break;
            }
        return moves;
    }

    public static ulong[] GenerateBlockerCombinations(ulong mask)
    {
        int numberOfCombinations = 1 << BitOperations.PopCount(mask);
        ulong[] result = new ulong[numberOfCombinations];
        for (int i = 0; i < numberOfCombinations; i++)
            result[i] = PlaceCombinationOnMask(mask, i);
        return result;
    }

    // used for generating rook and bishop (magic numbers)
    // generates a ulong where the bits of combination are placed on the 1-bits of mask
    public static ulong PlaceCombinationOnMask(ulong mask, int combination)
    {
        ulong result = 0;
        for (int i = 0; i < 64; i++)
        {
            if (BitBoardHelper.IsBitSet(mask, i))
            {
                if (BitBoardHelper.IsBitSet((ulong)combination, 0))
                    BitBoardHelper.SetIndex(ref result, i);
                combination >>= 1;
            }
        }
        return result;
    }
}