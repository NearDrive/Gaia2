using System.Threading.Tasks;
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
        int ticksPerEpisode,
        bool useParallel = false,
        int? maxDegreeOfParallelism = null)
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

        if (useParallel && maxDegreeOfParallelism.HasValue && maxDegreeOfParallelism.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
        }

        int inputCount = (simConfig.AgentVisionRays * 3) + 1;
        int outputCount = 1;

        EpisodeRunner runner = new(simConfig);

        int genomeCount = pop.Genomes.Count;
        double[] fitnesses = new double[genomeCount];
        double[] rawFitnesses = new double[genomeCount];
        double[] avgTicksSurvived = new double[genomeCount];
        double[] avgSuccessfulDrinks = new double[genomeCount];
        float[] avgThirst01 = new float[genomeCount];
        int[] nodeCounts = new int[genomeCount];
        int[] connectionCounts = new int[genomeCount];

        void EvaluateGenome(int i)
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
            rawFitnesses[i] = rawFitness;
            avgTicksSurvived[i] = ticksSurvivedSum / episodesPerGenome;
            avgSuccessfulDrinks[i] = successfulDrinksSum / episodesPerGenome;
            avgThirst01[i] = (float)(avgThirstSum / episodesPerGenome);
            nodeCounts[i] = genome.NodeCount;
            connectionCounts[i] = genome.ConnectionCount;
        }

        if (useParallel)
        {
            int degree = maxDegreeOfParallelism ?? Environment.ProcessorCount;
            Parallel.For(0, genomeCount, new ParallelOptions { MaxDegreeOfParallelism = degree }, EvaluateGenome);
        }
        else
        {
            for (int i = 0; i < genomeCount; i += 1)
            {
                EvaluateGenome(i);
            }
        }

        double totalFitness = 0.0;
        double worstFitness = double.MaxValue;
        double bestFitness = double.MinValue;
        int bestIndex = 0;
        Genome? bestGenome = null;
        int bestGenomeNodeCount = 0;
        int bestGenomeConnectionCount = 0;
        int bestTicksSurvived = 0;
        int bestSuccessfulDrinks = 0;
        float bestAvgThirst = 0f;
        GenomeFitnessScore? bestScore = null;

        for (int i = 0; i < genomeCount; i += 1)
        {
            double adjustedFitness = fitnesses[i];
            totalFitness += adjustedFitness;

            GenomeFitnessScore score = new(
                i,
                adjustedFitness,
                rawFitnesses[i],
                nodeCounts[i],
                connectionCounts[i]);

            if (!bestScore.HasValue || GenomeFitnessScore.Compare(score, bestScore.Value) < 0)
            {
                bestScore = score;
                bestFitness = score.FitnessAdjusted;
                bestIndex = score.Index;
                bestGenome = pop.Genomes[i];
                bestGenomeNodeCount = nodeCounts[i];
                bestGenomeConnectionCount = connectionCounts[i];
                bestTicksSurvived = (int)Math.Round(
                    avgTicksSurvived[i],
                    MidpointRounding.AwayFromZero);
                bestSuccessfulDrinks = (int)Math.Round(
                    avgSuccessfulDrinks[i],
                    MidpointRounding.AwayFromZero);
                bestAvgThirst = avgThirst01[i];
            }

            if (adjustedFitness < worstFitness)
            {
                worstFitness = adjustedFitness;
            }
        }

        double meanFitness = totalFitness / genomeCount;

        return new GenerationResult(
            pop.Generation,
            bestFitness,
            meanFitness,
            worstFitness,
            bestIndex,
            fitnesses,
            rawFitnesses,
            bestGenome,
            bestGenomeNodeCount,
            bestGenomeConnectionCount,
            bestTicksSurvived,
            bestSuccessfulDrinks,
            bestAvgThirst);
    }
}
