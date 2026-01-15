using Core.Evo;

namespace Core.Sim;

public sealed class Trainer
{
    private const double ComplexityNodePenalty = 0.01;
    private const double ComplexityConnectionPenalty = 0.001;
    private readonly GenomeBrainFactory _brainFactory = new();

    public GenerationResult RunGeneration(
        Population pop,
        int baseSeed,
        int episodesPerGenome,
        SimulationConfig simConfig,
        int ticksPerEpisode)
    {
        if (pop is null)
        {
            throw new ArgumentNullException(nameof(pop));
        }

        if (episodesPerGenome <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(episodesPerGenome));
        }

        if (ticksPerEpisode <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerEpisode));
        }

        int inputCount = (simConfig.AgentVisionRays * 3) + 1;
        int outputCount = 1;

        EpisodeRunner runner = new(simConfig);

        double[] fitnesses = new double[pop.Genomes.Count];
        double totalFitness = 0.0;
        double bestFitness = double.MinValue;
        double worstFitness = double.MaxValue;
        int bestIndex = 0;
        Genome? bestGenome = null;

        for (int i = 0; i < pop.Genomes.Count; i += 1)
        {
            Genome genome = pop.Genomes[i];
            IBrain brain = _brainFactory.CreateBrain(genome, inputCount, outputCount);
            double fitnessSum = 0.0;

            for (int episode = 0; episode < episodesPerGenome; episode += 1)
            {
                int seed = baseSeed + (i * 1000) + episode;
                EpisodeResult result = runner.RunEpisode(brain, seed, ticksPerEpisode, 1);
                fitnessSum += result.Fitness;
            }

            double rawFitness = fitnessSum / episodesPerGenome;
            double penalty = (ComplexityNodePenalty * genome.NodeCount)
                + (ComplexityConnectionPenalty * genome.ConnectionCount);
            double adjustedFitness = rawFitness - penalty;
            fitnesses[i] = adjustedFitness;
            totalFitness += adjustedFitness;

            if (adjustedFitness > bestFitness)
            {
                bestFitness = adjustedFitness;
                bestIndex = i;
                bestGenome = genome;
            }

            if (adjustedFitness < worstFitness)
            {
                worstFitness = adjustedFitness;
            }
        }

        double meanFitness = totalFitness / pop.Genomes.Count;

        return new GenerationResult(
            pop.Generation,
            bestFitness,
            meanFitness,
            worstFitness,
            bestIndex,
            fitnesses,
            bestGenome);
    }
}
