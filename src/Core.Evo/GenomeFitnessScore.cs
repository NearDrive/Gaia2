namespace Core.Evo;

public readonly record struct GenomeFitnessScore(
    int Index,
    double FitnessAdjusted,
    double FitnessRaw,
    int NodeCount,
    int ConnectionCount)
{
    public int Complexity => NodeCount + ConnectionCount;

    public static int Compare(GenomeFitnessScore a, GenomeFitnessScore b)
    {
        int adjustedCompare = b.FitnessAdjusted.CompareTo(a.FitnessAdjusted);
        if (adjustedCompare != 0)
        {
            return adjustedCompare;
        }

        int rawCompare = b.FitnessRaw.CompareTo(a.FitnessRaw);
        if (rawCompare != 0)
        {
            return rawCompare;
        }

        int complexityCompare = a.Complexity.CompareTo(b.Complexity);
        if (complexityCompare != 0)
        {
            return complexityCompare;
        }

        return a.Index.CompareTo(b.Index);
    }
}
