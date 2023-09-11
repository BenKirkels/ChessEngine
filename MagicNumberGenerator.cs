namespace MagicGenerator;
using Chess;
using System.Numerics;

public static class MagicNumberGenerator
{
    static ulong[] masks = new ulong[64];
    public static void magicGenerator(bool rookMoves)
    {
        int[] shifts = rookMoves ? MagicNumbers.rookShift : MagicNumbers.bishopShift;
        for (int i = 0; i < 64; i++)
        {
            masks[i] = MagicHelper.GenerateSliderMask(i, rookMoves);
        }

        ulong[] magicNumbers = new ulong[64];

        Random random = new Random();
        HashSet<int> used = new HashSet<int>();

        int[] foundNumbers = new int[64];

        while (true)
        {
            ulong newMagicNumber = Zobrist.Random64bitNumber(random) & Zobrist.Random64bitNumber(random) & Zobrist.Random64bitNumber(random);
            for (int index = 0; index < 64; index++)
            {
                if (foundNumbers[index] == 1)
                    continue;
                ulong mask = masks[index];

                int maxCount = 0;
                bool valid = true;
                used.Clear();

                int totalCombinations = (1 << BitOperations.PopCount(mask)) - 1;

                for (int combination = 0; combination <= totalCombinations; combination++)
                {
                    ulong bitboard = MagicHelper.PlaceCombinationOnMask(mask, combination);

                    int count = (int)(uint)((bitboard * newMagicNumber) >> shifts[index]);
                    if (used.Contains(count))
                    {
                        valid = false;
                        break; // This magic number tries to map two bitboards to the same index or it's worse than the current best
                    }
                    used.Add(count);
                    if (count > maxCount)
                        maxCount = count;
                }
                if (valid)
                {
                    Console.WriteLine($"index: {index}");
                    foundNumbers[index] = 1;
                    magicNumbers[index] = newMagicNumber;
                }
            }
            if (foundNumbers.Sum() == 64)
            {
                Console.WriteLine($"{{ {string.Join(", ", magicNumbers)} }}");
                break;
            }
        }
    }
}