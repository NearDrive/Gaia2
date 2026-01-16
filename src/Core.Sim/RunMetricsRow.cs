namespace Core.Sim;

public sealed record RunMetricsRow
{
    public int Index { get; init; }
    public float BestFitness { get; init; }
    public float AvgFitness { get; init; }
    public float P50Fitness { get; init; }
    public float P90Fitness { get; init; }
    public int AliveAtEnd { get; init; }
    public ulong FinalChecksum { get; init; }
}
