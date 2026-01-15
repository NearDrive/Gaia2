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
        Genome? bestGenome,
        int bestGenomeNodeCount,
        int bestGenomeConnectionCount,
        int bestTicksSurvived,
        int bestSuccessfulDrinks,
        float bestAvgThirst01)
    {
        Generation = generation;
        BestFitness = bestFitness;
        MeanFitness = meanFitness;
        WorstFitness = worstFitness;
        BestGenomeIndex = bestGenomeIndex;
        Fitnesses = fitnesses ?? throw new ArgumentNullException(nameof(fitnesses));
        BestGenome = bestGenome;
        BestGenomeNodeCount = bestGenomeNodeCount;
        BestGenomeConnectionCount = bestGenomeConnectionCount;
        BestTicksSurvived = bestTicksSurvived;
        BestSuccessfulDrinks = bestSuccessfulDrinks;
        BestAvgThirst01 = bestAvgThirst01;
    }

    public int Generation { get; }

    public double BestFitness { get; }

    public double MeanFitness { get; }

    public double WorstFitness { get; }

    public int BestGenomeIndex { get; }

    public IReadOnlyList<double> Fitnesses { get; }

    public Genome? BestGenome { get; }

    public int BestGenomeNodeCount { get; }

    public int BestGenomeConnectionCount { get; }

    public int BestTicksSurvived { get; }

    public int BestSuccessfulDrinks { get; }

    public float BestAvgThirst01 { get; }
}
