using Core.Evo;
using Xunit;

namespace Core.Sim.Tests;

public class EvolutionDeterminismTests
{
    [Fact]
    public void Evolution_SameSeed_SameBestFitness_AfterNGenerations()
    {
        double bestFirst = RunEvolution(seed: 123);
        double bestSecond = RunEvolution(seed: 123);

        Assert.Equal(bestFirst, bestSecond);
    }

    private static double RunEvolution(int seed)
    {
        EvolutionConfig config = new(
            populationSize: 10,
            eliteCount: 2,
            mutationRate: 0.9,
            addConnectionRate: 0.2,
            addNodeRate: 0.05,
            weightPerturbRate: 0.8,
            weightResetRate: 0.1,
            maxNodes: 16,
            maxConnections: 32);

        Evolver evolver = new(config);
        SimulationConfig simConfig = new(
            seed: seed,
            dt: 1f,
            ticksPerEpisode: 50,
            worldWidth: 16,
            worldHeight: 16,
            agentVisionRays: 4,
            agentVisionRange: 6f,
            agentFov: MathF.PI / 2f);

        int inputCount = BrainIO.InputCount(simConfig.AgentVisionRays, simConfig.EmbeddingDimension);
        int outputCount = BrainIO.OutputCount;

        Population population = evolver.CreateInitialPopulation(seed, config.PopulationSize, inputCount, outputCount);
        Trainer trainer = new();
        CurriculumSchedule curriculum = new(
            totalGenerations: 3,
            start: new CurriculumPhase(
                WaterProximityBias01: 1f,
                ObstacleDensity01: 0.1f,
                WorldSize: 16,
                TicksPerEpisode: 50,
                ThirstRatePerSecond: simConfig.ThirstRatePerSecond),
            end: new CurriculumPhase(
                WaterProximityBias01: 0f,
                ObstacleDensity01: 0.4f,
                WorldSize: 20,
                TicksPerEpisode: 50,
                ThirstRatePerSecond: simConfig.ThirstRatePerSecond * 1.1f));

        double bestFitness = double.MinValue;
        for (int generation = 0; generation < 3; generation += 1)
        {
            GenerationResult result = trainer.RunGeneration(
                population,
                baseSeed: seed + (generation * 1000),
                episodesPerGenome: 1,
                simConfig,
                curriculum);

            bestFitness = result.BestFitness;
            population = evolver.NextGeneration(population, result.Fitnesses, seed + generation + 1);
        }

        return bestFitness;
    }
}
