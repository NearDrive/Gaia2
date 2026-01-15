using Core.Evo;

namespace Core.Sim;

public sealed class GenerationResult
{
    public GenerationResult(
        int generation,
        double bestFitness,
        double meanFitness,
        double worstFitness,
        int bestGenomeIndex,
        IReadOnlyList<double> fitnesses,
        IReadOnlyList<double> rawFitnesses,
        Genome? bestGenome,
        int bestGenomeNodeCount,
        int bestGenomeConnectionCount,
        int bestTicksSurvived,
        int bestSuccessfulDrinks,
        float bestAvgThirst01,
        double bestDistanceTraveled,
        int bestVisitedCells,
        int bestDrinkableCellsVisited,
        double meanVisitedCells,
        int worldSize,
        float obstacleDensity01,
        float waterProximityBias01,
        int ticksPerEpisode)
    {
        Generation = generation;
        BestFitness = bestFitness;
        MeanFitness = meanFitness;
        WorstFitness = worstFitness;
        BestGenomeIndex = bestGenomeIndex;
        Fitnesses = fitnesses ?? throw new ArgumentNullException(nameof(fitnesses));
        RawFitnesses = rawFitnesses ?? throw new ArgumentNullException(nameof(rawFitnesses));
        BestGenome = bestGenome;
        BestGenomeNodeCount = bestGenomeNodeCount;
        BestGenomeConnectionCount = bestGenomeConnectionCount;
        BestTicksSurvived = bestTicksSurvived;
        BestSuccessfulDrinks = bestSuccessfulDrinks;
        BestAvgThirst01 = bestAvgThirst01;
        BestDistanceTraveled = bestDistanceTraveled;
        BestVisitedCells = bestVisitedCells;
        BestDrinkableCellsVisited = bestDrinkableCellsVisited;
        MeanVisitedCells = meanVisitedCells;
        WorldSize = worldSize;
        ObstacleDensity01 = obstacleDensity01;
        WaterProximityBias01 = waterProximityBias01;
        TicksPerEpisode = ticksPerEpisode;
    }

    public int Generation { get; }

    public double BestFitness { get; }

    public double MeanFitness { get; }

    public double WorstFitness { get; }

    public int BestGenomeIndex { get; }

    public IReadOnlyList<double> Fitnesses { get; }

    public IReadOnlyList<double> RawFitnesses { get; }

    public Genome? BestGenome { get; }

    public int BestGenomeNodeCount { get; }

    public int BestGenomeConnectionCount { get; }

    public int BestTicksSurvived { get; }

    public int BestSuccessfulDrinks { get; }

    public float BestAvgThirst01 { get; }

    public double BestDistanceTraveled { get; }

    public int BestVisitedCells { get; }

    public int BestDrinkableCellsVisited { get; }

    public double MeanVisitedCells { get; }

    public int WorldSize { get; }

    public float ObstacleDensity01 { get; }

    public float WaterProximityBias01 { get; }

    public int TicksPerEpisode { get; }
}
