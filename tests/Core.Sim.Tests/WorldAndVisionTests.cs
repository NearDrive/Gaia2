using System.Numerics;
using Core.Evo;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class WorldAndVisionTests
{
    [Fact]
    public void World_SameSeed_SameWorldChecksum()
    {
        const int seed = 999;
        GridWorld first = new(32, 32, seed);
        GridWorld second = new(32, 32, seed);

        Assert.Equal(first.ComputeChecksum(), second.ComputeChecksum());
    }

    [Fact]
    public void World_DifferentSeed_DifferentWorldChecksum()
    {
        GridWorld first = new(32, 32, 111);
        GridWorld second = new(32, 32, 222);

        Assert.NotEqual(first.ComputeChecksum(), second.ComputeChecksum());
    }

    [Fact]
    public void Vision_SameSeed_SameVisionOutput()
    {
        const int seed = 42;
        GridWorld worldA = new(32, 32, seed);
        GridWorld worldB = new(32, 32, seed);
        EmbeddingRegistry embeddings = new(dimension: 8, seed: 101);
        VisionSensor sensor = new(8, 10f, MathF.PI / 2f, embeddings);
        Vector2 position = new(16.5f, 16.5f);

        float[] first = sensor.Sense(worldA, position, 0f);
        float[] second = sensor.Sense(worldB, position, 0f);

        Assert.Equal(first.Length, second.Length);
        for (int i = 0; i < first.Length; i += 1)
        {
            Assert.Equal(first[i], second[i]);
        }

        Assert.Equal(BrainIO.VisionInputCount(8, embeddings.Dimension), first.Length);
    }

    [Fact]
    public void Vision_OutputSize_MatchesBrainIOContract()
    {
        EmbeddingRegistry embeddings = new(dimension: 6, seed: 11);
        VisionSensor sensor = new(4, 6f, MathF.PI / 3f, embeddings);
        GridWorld world = new(16, 16, 123);
        Vector2 position = new(8.5f, 8.5f);

        float[] output = sensor.Sense(world, position, 0f);

        Assert.Equal(BrainIO.VisionInputCount(4, 6), output.Length);
    }

    [Fact]
    public void BrainIO_InputCount_AlignsWithSimulationVisionBuffer()
    {
        SimulationConfig config = new(
            seed: 3,
            dt: 1f,
            ticksPerEpisode: 10,
            worldWidth: 16,
            worldHeight: 16,
            agentVisionRays: 5,
            agentVisionRange: 6f,
            agentFov: MathF.PI / 2f,
            embeddingDimension: 5,
            embeddingSeed: 7);
        Simulation simulation = new(config, 1);
        int visionCount = BrainIO.VisionInputCount(config.AgentVisionRays, config.EmbeddingDimension);

        Assert.Equal(visionCount, simulation.Agents[0].LastVision.Length);
        Assert.Equal(
            BrainIO.InputCount(config.AgentVisionRays, config.EmbeddingDimension),
            visionCount + BrainIO.NonVisionInputCount);
    }

    [Fact]
    public void Agent_CannotEnterBlockedTiles()
    {
        GridWorld world = new(32, 32, 123);
        (Vector2 start, Vector2 delta) = FindBlockedMove(world);
        AgentState agent = new(start);

        bool moved = agent.TryApplyDelta(world, delta);

        Assert.False(moved);
        Assert.Equal(start, agent.Position);
    }

    private static (Vector2 Start, Vector2 Delta) FindBlockedMove(GridWorld world)
    {
#pragma warning disable CA2014
        (int dx, int dy)[] neighbors =
        {
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1)
        };
#pragma warning restore CA2014

        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                if (!world.IsBlocked(x, y))
                {
                    continue;
                }

                foreach ((int dx, int dy) in neighbors)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!world.InBounds(nx, ny))
                    {
                        continue;
                    }

                    if (world.IsBlocked(nx, ny))
                    {
                        continue;
                    }

                    Vector2 start = new(nx + 0.5f, ny + 0.5f);
                    Vector2 delta = new(dx * -1f, dy * -1f);
                    return (start, delta);
                }
            }
        }

        throw new InvalidOperationException("No blocked tile with an empty neighbor found.");
    }
}
