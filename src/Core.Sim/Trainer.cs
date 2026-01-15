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
        int bestGenomeNodeCount = 0;
        int bestGenomeConnectionCount = 0;
        int bestTicksSurvived = 0;
        int bestSuccessfulDrinks = 0;
        float bestAvgThirst01 = 0f;

        for (int i = 0; i < pop.Genomes.Count; i += 1)
        {
            Genome genome = pop.Genomes[i];
            IBrain brain = _brainFactory.CreateBrain(genome, inputCount, outputCount);
            double fitnessSum = 0.0;
            double ticksSurvivedSum = 0.0;
            double successfulDrinksSum = 0.0;
            double avgThirstSum = 0.0;

            for (int episode = 0; episode < episodesPerGenome; episode += 1)
            {
                int seed = baseSeed + (i * 1000) + episode;
                EpisodeResult result = runner.RunEpisode(brain, seed, ticksPerEpisode, 1);
                fitnessSum += result.Fitness;
                ticksSurvivedSum += result.TicksSurvived;
                successfulDrinksSum += result.SuccessfulDrinks;
                avgThirstSum += result.AvgThirst01;
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
                bestGenomeNodeCount = genome.NodeCount;
                bestGenomeConnectionCount = genome.ConnectionCount;
                bestTicksSurvived = (int)Math.Round(
                    ticksSurvivedSum / episodesPerGenome,
                    MidpointRounding.AwayFromZero);
                bestSuccessfulDrinks = (int)Math.Round(
                    successfulDrinksSum / episodesPerGenome,
                    MidpointRounding.AwayFromZero);
                bestAvgThirst01 = (float)(avgThirstSum / episodesPerGenome);
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
            bestGenome,
            bestGenomeNodeCount,
            bestGenomeConnectionCount,
            bestTicksSurvived,
            bestSuccessfulDrinks,
            bestAvgThirst01);
    }
}
