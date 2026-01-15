namespace Core.Evo;

public sealed class EvolutionConfig
{
    public EvolutionConfig(
        int populationSize,
        int eliteCount,
        double mutationRate,
        double addConnectionRate,
        double addNodeRate,
        double weightPerturbRate,
        double weightResetRate,
        int maxNodes,
        int maxConnections)
    {
        if (populationSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(populationSize));
        }

        if (eliteCount < 0 || eliteCount > populationSize)
        {
            throw new ArgumentOutOfRangeException(nameof(eliteCount));
        }

        if (maxNodes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNodes));
        }

        if (maxConnections <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConnections));
        }

        PopulationSize = populationSize;
        EliteCount = eliteCount;
        MutationRate = mutationRate;
        AddConnectionRate = addConnectionRate;
        AddNodeRate = addNodeRate;
        WeightPerturbRate = weightPerturbRate;
        WeightResetRate = weightResetRate;
        MaxNodes = maxNodes;
        MaxConnections = maxConnections;
    }

    public int PopulationSize { get; }

    public int EliteCount { get; }

    public double MutationRate { get; }

    public double AddConnectionRate { get; }

    public double AddNodeRate { get; }

    public double WeightPerturbRate { get; }

    public double WeightResetRate { get; }

    public int MaxNodes { get; }

    public int MaxConnections { get; }
}
