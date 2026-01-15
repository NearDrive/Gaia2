using System.Numerics;
using Core.Evo;
using Xunit;

namespace Core.Sim.Tests;

public class CurriculumTests
{
    [Fact]
    public void CurriculumPhase_ChangesOverGenerations()
    {
        CurriculumSchedule schedule = new(
            totalGenerations: 10,
            start: new CurriculumPhase(
                WaterProximityBias01: 1f,
                ObstacleDensity01: 0.1f,
                WorldSize: 16,
                TicksPerEpisode: 50,
                ThirstRatePerSecond: 0.01f),
            end: new CurriculumPhase(
                WaterProximityBias01: 0f,
                ObstacleDensity01: 0.4f,
                WorldSize: 24,
                TicksPerEpisode: 100,
                ThirstRatePerSecond: 0.02f));

        CurriculumPhase first = schedule.GetPhase(0);
        CurriculumPhase last = schedule.GetPhase(schedule.TotalGenerations - 1);

        Assert.NotEqual(first, last);
        Assert.True(first.ObstacleDensity01 < last.ObstacleDensity01);
        Assert.True(first.WaterProximityBias01 > last.WaterProximityBias01);
    }

    [Fact]
    public void WorldGen_Bias1_SpawnsNearWater()
    {
        SimulationConfig config = new(
            Seed: 42,
            Dt: 1f,
            TicksPerEpisode: 50,
            WorldWidth: 20,
            WorldHeight: 20,
            AgentVisionRays: 4,
            AgentVisionRange: 6f,
            AgentFov: MathF.PI / 2f,
            WaterProximityBias01: 1f,
            ObstacleDensity01: 0.1f);

        Simulation simulation = new(config, agentCount: 1);
        Vector2 position = simulation.Agents[0].Position;

        Assert.True(HasWaterWithinRadius(simulation.World, position, radius: 3));
    }

    [Fact]
    public void Determinism_CurriculumSequence_SameSeed()
    {
        CurriculumSchedule schedule = new(
            totalGenerations: 3,
            start: new CurriculumPhase(
                WaterProximityBias01: 1f,
                ObstacleDensity01: 0.1f,
                WorldSize: 12,
                TicksPerEpisode: 12,
                ThirstRatePerSecond: 0.01f),
            end: new CurriculumPhase(
                WaterProximityBias01: 0f,
                ObstacleDensity01: 0.4f,
                WorldSize: 16,
                TicksPerEpisode: 14,
                ThirstRatePerSecond: 0.012f));

        (CurriculumPhase[] phases, ulong[] checksums) firstRun = RunCurriculumSequence(seed: 99, schedule);
        (CurriculumPhase[] phases, ulong[] checksums) secondRun = RunCurriculumSequence(seed: 99, schedule);

        Assert.Equal(firstRun.phases, secondRun.phases);
        Assert.Equal(firstRun.checksums, secondRun.checksums);
    }

    private static (CurriculumPhase[] phases, ulong[] checksums) RunCurriculumSequence(
        int seed,
        CurriculumSchedule schedule)
    {
        CurriculumPhase[] phases = new CurriculumPhase[schedule.TotalGenerations];
        ulong[] checksums = new ulong[schedule.TotalGenerations];
        IBrain brain = new NoopBrain();

        for (int generation = 0; generation < schedule.TotalGenerations; generation += 1)
        {
            CurriculumPhase phase = schedule.GetPhase(generation);
            phases[generation] = phase;

            SimulationConfig config = new(
                Seed: seed,
                Dt: 1f,
                TicksPerEpisode: phase.TicksPerEpisode,
                WorldWidth: phase.WorldSize,
                WorldHeight: phase.WorldSize,
                AgentVisionRays: 4,
                AgentVisionRange: 6f,
                AgentFov: MathF.PI / 2f,
                ThirstRatePerSecond: phase.ThirstRatePerSecond,
                WaterProximityBias01: phase.WaterProximityBias01,
                ObstacleDensity01: phase.ObstacleDensity01);

            EpisodeRunner runner = new(config);
            EpisodeResult result = runner.RunEpisode(brain, seed + (generation * 100), phase.TicksPerEpisode, 1);
            checksums[generation] = result.Checksum;
        }

        return (phases, checksums);
    }

    private static bool HasWaterWithinRadius(GridWorld world, Vector2 position, int radius)
    {
        int tileX = (int)MathF.Floor(position.X);
        int tileY = (int)MathF.Floor(position.Y);

        for (int dy = -radius; dy <= radius; dy += 1)
        {
            for (int dx = -radius; dx <= radius; dx += 1)
            {
                int nx = tileX + dx;
                int ny = tileY + dy;

                if (!world.InBounds(nx, ny))
                {
                    continue;
                }

                if (world.GetTile(nx, ny).Id == TileId.Water)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed class NoopBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput();
        }
    }
}
