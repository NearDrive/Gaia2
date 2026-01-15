using System.Linq;
using Xunit;

namespace Core.Evo.Tests;

public class CheckpointTests
{
    [Fact]
    public void Checkpoint_Roundtrip_PreservesGenome()
    {
        Genome genome = new(
            new[]
            {
                new NodeGene(2, NodeType.Hidden),
                new NodeGene(0, NodeType.Input),
                new NodeGene(1, NodeType.Output)
            },
            new[]
            {
                new ConnectionGene(0, 1, 0.5, true, 3),
                new ConnectionGene(2, 1, -0.25, false, 1),
                new ConnectionGene(0, 2, 0.75, true, 2)
            });

        string json = GenomeJson.Serialize(genome);
        Genome roundtrip = GenomeJson.Deserialize(json);

        Assert.Equal(genome.NodeCount, roundtrip.NodeCount);
        Assert.Equal(genome.ConnectionCount, roundtrip.ConnectionCount);
        Assert.True(GenomeEquals(genome, roundtrip));
    }

    [Fact]
    public void Resume_ProducesSameNextGeneration_AsContinuousRun()
    {
        EvolutionConfig config = new(
            populationSize: 6,
            eliteCount: 1,
            mutationRate: 0.9,
            addConnectionRate: 0.5,
            addNodeRate: 0.2,
            weightPerturbRate: 0.8,
            weightResetRate: 0.2,
            maxNodes: 16,
            maxConnections: 32);

        Evolver evolverA = new(config);
        Population popA = evolverA.CreateInitialPopulation(seed: 123, populationSize: config.PopulationSize, inputCount: 3, outputCount: 1);
        EvoRng rngA = new(999);

        double[] fitnessesA = Enumerable.Range(0, config.PopulationSize).Select(i => (double)i).ToArray();
        Population popA1 = evolverA.NextGeneration(popA, fitnessesA, rngA);
        Population popA2 = evolverA.NextGeneration(popA1, fitnessesA, rngA);

        Evolver evolverB = new(config);
        Population popB = evolverB.CreateInitialPopulation(seed: 123, populationSize: config.PopulationSize, inputCount: 3, outputCount: 1);
        EvoRng rngB = new(999);
        Population popB1 = evolverB.NextGeneration(popB, fitnessesA, rngB);

        RngState checkpointState = rngB.GetState();
        EvoRng resumedRng = new(checkpointState);
        Population popB2 = evolverB.NextGeneration(popB1, fitnessesA, resumedRng);

        Assert.Equal(popA2.Genomes.Count, popB2.Genomes.Count);
        for (int i = 0; i < popA2.Genomes.Count; i += 1)
        {
            Assert.True(GenomeEquals(popA2.Genomes[i], popB2.Genomes[i]));
        }
    }

    private static bool GenomeEquals(Genome a, Genome b)
    {
        if (a.NodeCount != b.NodeCount || a.ConnectionCount != b.ConnectionCount)
        {
            return false;
        }

        NodeGene[] nodesA = a.Nodes.OrderBy(node => node.Id).ToArray();
        NodeGene[] nodesB = b.Nodes.OrderBy(node => node.Id).ToArray();
        for (int i = 0; i < nodesA.Length; i += 1)
        {
            if (nodesA[i] != nodesB[i])
            {
                return false;
            }
        }

        ConnectionGene[] connectionsA = a.Connections.OrderBy(connection => connection.InnovationId).ToArray();
        ConnectionGene[] connectionsB = b.Connections.OrderBy(connection => connection.InnovationId).ToArray();
        for (int i = 0; i < connectionsA.Length; i += 1)
        {
            if (connectionsA[i] != connectionsB[i])
            {
                return false;
            }
        }

        return true;
    }
}
