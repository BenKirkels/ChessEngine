using Helpers;
using Chess;

namespace Evaluation;

public static class PawnMasks
{
    public static readonly ulong[,] passedPawn = new ulong[2, 64];
    public static readonly ulong[,] isolatedPawn = new ulong[2, 64];
    //public static readonly ulong[,] backwardPawn = new ulong[2, 64];
    public static readonly ulong[,] doubledPawn = new ulong[2, 64];

    static PawnMasks()
    {
        for (int index = 0; index < 64; index++)
        {
            int file = BoardHelper.IndexToFile(index);
            int rank = BoardHelper.IndexToRank(index);

            ulong fileMask = BitBoardHelper.File(file);
            ulong leftFileMask = file != 0 ? BitBoardHelper.File(file - 1) : 0;
            ulong rightFileMask = file != 7 ? BitBoardHelper.File(file + 1) : 0;

            ulong passedPawnMask = fileMask | leftFileMask | rightFileMask;
            passedPawn[Board.WHITE_INDEX, index] = passedPawnMask << ((rank + 1) * 8);
            passedPawn[Board.BLACK_INDEX, index] = passedPawnMask >> ((8 - rank) * 8);

            ulong isolatedPawnMask = leftFileMask | rightFileMask;
            isolatedPawn[Board.WHITE_INDEX, index] = isolatedPawnMask;
            isolatedPawn[Board.BLACK_INDEX, index] = isolatedPawnMask;

            ulong doubledPawnMask = fileMask;
            doubledPawn[Board.WHITE_INDEX, index] = doubledPawnMask << ((rank + 1) * 8);
            doubledPawn[Board.BLACK_INDEX, index] = doubledPawnMask >> ((8 - rank) * 8);
        }
    }
}