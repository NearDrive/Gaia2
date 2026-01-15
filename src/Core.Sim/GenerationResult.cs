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
        Genome? bestGenome)
    {
        Generation = generation;
        BestFitness = bestFitness;
        MeanFitness = meanFitness;
        WorstFitness = worstFitness;
        BestGenomeIndex = bestGenomeIndex;
        Fitnesses = fitnesses ?? throw new ArgumentNullException(nameof(fitnesses));
        BestGenome = bestGenome;
    }

    public int Generation { get; }

    public double BestFitness { get; }

    public double MeanFitness { get; }

    public double WorstFitness { get; }

    public int BestGenomeIndex { get; }

    public IReadOnlyList<double> Fitnesses { get; }

    public Genome? BestGenome { get; }
}
