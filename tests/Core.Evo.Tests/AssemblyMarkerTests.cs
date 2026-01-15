using System.Linq;
using Xunit;

namespace Core.Evo.Tests;

public class AssemblyMarkerTests
{
    [Fact]
    public void BloatCaps_AreEnforced()
    {
        EvolutionConfig config = new(
            populationSize: 10,
            eliteCount: 0,
            mutationRate: 1.0,
            addConnectionRate: 1.0,
            addNodeRate: 1.0,
            weightPerturbRate: 1.0,
            weightResetRate: 0.0,
            maxNodes: 6,
            maxConnections: 8);

        Evolver evolver = new(config);
        Population population = evolver.CreateInitialPopulation(seed: 42, populationSize: config.PopulationSize, inputCount: 3, outputCount: 1);

        for (int gen = 0; gen < 10; gen += 1)
        {
            double[] fitnesses = Enumerable.Repeat(100.0, population.Genomes.Count).ToArray();
            population = evolver.NextGeneration(population, fitnesses, seed: 100 + gen);
        }

        foreach (Genome genome in population.Genomes)
        {
            Assert.InRange(genome.NodeCount, 0, config.MaxNodes);
            Assert.InRange(genome.ConnectionCount, 0, config.MaxConnections);
        }
    }

    [Fact]
    public void Mutation_ChangesGenome_Sometimes()
    {
        EvolutionConfig config = new(
            populationSize: 4,
            eliteCount: 0,
            mutationRate: 1.0,
            addConnectionRate: 1.0,
            addNodeRate: 1.0,
            weightPerturbRate: 1.0,
            weightResetRate: 0.0,
            maxNodes: 12,
            maxConnections: 24);

        Evolver evolver = new(config);
        Population population = evolver.CreateInitialPopulation(seed: 7, populationSize: config.PopulationSize, inputCount: 3, outputCount: 1);
        double[] fitnesses = Enumerable.Repeat(1.0, population.Genomes.Count).ToArray();

        Population next = evolver.NextGeneration(population, fitnesses, seed: 99);

        bool changed = false;
        for (int i = 0; i < population.Genomes.Count; i += 1)
        {
            if (!GenomeEquals(population.Genomes[i], next.Genomes[i]))
            {
                changed = true;
                break;
            }
        }

        Assert.True(changed);
    }

    private static bool GenomeEquals(Genome a, Genome b)
    {
        if (a.NodeCount != b.NodeCount || a.ConnectionCount != b.ConnectionCount)
        {
            return false;
        }

        for (int i = 0; i < a.Nodes.Count; i += 1)
        {
            if (a.Nodes[i] != b.Nodes[i])
            {
                return false;
            }
        }

        for (int i = 0; i < a.Connections.Count; i += 1)
        {
            if (a.Connections[i] != b.Connections[i])
            {
                return false;
            }
        }

        return true;
    }
}
