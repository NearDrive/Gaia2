using System.Linq;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class SnapshotTests
{
    [Fact]
    public void Snapshot_IsDeterministic()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new NoOpBrain();
        const int snapshotEvery = 10;
        const int snapshotTick = 50;

        EpisodeResult firstResult = runner.RunEpisode(
            brain,
            12345,
            60,
            2,
            captureSnapshots: true,
            snapshotEveryNTicks: snapshotEvery);
        Assert.True(firstResult.SnapshotsEnabled);
        Assert.Equal(snapshotEvery, firstResult.SnapshotEveryNTicks);
        Assert.True(firstResult.SnapshotCount > 0);

        WorldSnapshot first = FindSnapshot(firstResult.Snapshots, snapshotTick);
        string firstJson = SnapshotJson.Serialize(first);

        EpisodeResult secondResult = runner.RunEpisode(
            brain,
            12345,
            60,
            2,
            captureSnapshots: true,
            snapshotEveryNTicks: snapshotEvery);
        Assert.True(secondResult.SnapshotsEnabled);
        Assert.Equal(snapshotEvery, secondResult.SnapshotEveryNTicks);
        Assert.True(secondResult.SnapshotCount > 0);

        WorldSnapshot second = FindSnapshot(secondResult.Snapshots, snapshotTick);
        string secondJson = SnapshotJson.Serialize(second);

        Assert.Equal(first.Header.WorldChecksum, second.Header.WorldChecksum);
        Assert.Equal(first.Header.AgentCount, second.Header.AgentCount);
        Assert.Equal(firstJson, secondJson);
    }

    [Fact]
    public void Snapshot_AgentCountMatches()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new NoOpBrain();
        const int snapshotEvery = 10;
        const int snapshotTick = 30;

        EpisodeResult result = runner.RunEpisode(
            brain,
            777,
            60,
            3,
            captureSnapshots: true,
            snapshotEveryNTicks: snapshotEvery);

        Assert.True(result.SnapshotsEnabled);
        Assert.Equal(snapshotEvery, result.SnapshotEveryNTicks);
        Assert.True(result.SnapshotCount > 0);

        WorldSnapshot snapshot = FindSnapshot(result.Snapshots, snapshotTick);

        Assert.Equal(snapshot.Header.AgentCount, snapshot.Agents.Count);
    }

    private static SimulationConfig CreateConfig(float thirstRate = 0.05f, float deathGrace = 2f)
    {
        return new SimulationConfig(
            0,
            1f,
            100,
            8,
            8,
            4,
            4f,
            MathF.PI / 2f,
            1.5f,
            0.05f,
            thirstRate,
            deathGrace);
    }

    private static WorldSnapshot FindSnapshot(IReadOnlyList<WorldSnapshot> snapshots, int tick)
    {
        foreach (WorldSnapshot snapshot in snapshots)
        {
            if (snapshot.Header.Tick == tick)
            {
                return snapshot;
            }
        }

        string captured = string.Join(",", snapshots.Select(s => s.Header.Tick).Take(50));
        throw new InvalidOperationException(
            $"Snapshot for tick {tick} was not found. CapturedTicks=[{captured}] Count={snapshots.Count}");
    }

}
