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
            Seed: seed,
            Dt: 1f,
            TicksPerEpisode: 50,
            WorldWidth: 16,
            WorldHeight: 16,
            AgentVisionRays: 4,
            AgentVisionRange: 6f,
            AgentFov: MathF.PI / 2f);

        int inputCount = (simConfig.AgentVisionRays * 3) + 2;
        int outputCount = 3;

        Population population = evolver.CreateInitialPopulation(seed, config.PopulationSize, inputCount, outputCount);
        Trainer trainer = new();

        double bestFitness = double.MinValue;
        for (int generation = 0; generation < 3; generation += 1)
        {
            GenerationResult result = trainer.RunGeneration(
                population,
                baseSeed: seed + (generation * 1000),
                episodesPerGenome: 1,
                simConfig,
                ticksPerEpisode: 50);

            bestFitness = result.BestFitness;
            population = evolver.NextGeneration(population, result.Fitnesses, seed + generation + 1);
        }

        return bestFitness;
    }
}
