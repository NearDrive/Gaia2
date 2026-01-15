using System.Globalization;
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
        IBrain brain = new AlwaysDrinkBrain();
        string snapshotDirectory = Path.Combine("artifacts", "snapshots");
        const int snapshotEvery = 50;

        CleanSnapshots(snapshotDirectory);
        runner.RunEpisode(brain, 12345, 60, 2, captureSnapshots: true, snapshotEveryNTicks: snapshotEvery);
        string firstJson = ReadSnapshotJson(snapshotDirectory, snapshotEvery);
        WorldSnapshot first = SnapshotJson.Deserialize(firstJson);

        CleanSnapshots(snapshotDirectory);
        runner.RunEpisode(brain, 12345, 60, 2, captureSnapshots: true, snapshotEveryNTicks: snapshotEvery);
        string secondJson = ReadSnapshotJson(snapshotDirectory, snapshotEvery);
        WorldSnapshot second = SnapshotJson.Deserialize(secondJson);

        Assert.Equal(first.Header.WorldChecksum, second.Header.WorldChecksum);
        Assert.Equal(first.Header.AgentCount, second.Header.AgentCount);
        Assert.Equal(firstJson, secondJson);
    }

    [Fact]
    public void Snapshot_AgentCountMatches()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new AlwaysDrinkBrain();
        string snapshotDirectory = Path.Combine("artifacts", "snapshots");
        const int snapshotEvery = 30;

        CleanSnapshots(snapshotDirectory);
        runner.RunEpisode(brain, 777, 60, 3, captureSnapshots: true, snapshotEveryNTicks: snapshotEvery);

        string json = ReadSnapshotJson(snapshotDirectory, snapshotEvery);
        WorldSnapshot snapshot = SnapshotJson.Deserialize(json);

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

    private static string ReadSnapshotJson(string directory, int tick)
    {
        string path = Path.Combine(directory, $"snap_{tick.ToString(CultureInfo.InvariantCulture)}.json");
        return File.ReadAllText(path);
    }

    private static void CleanSnapshots(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    private sealed class AlwaysDrinkBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput
            {
                ActionDrinkScore = 1f
            };
        }
    }
}
