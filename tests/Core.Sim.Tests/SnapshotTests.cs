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
        IBrain brain = new NoOpBrain();
        const int snapshotEvery = 50;
        string outDir = CreateSnapshotDirectory();

        try
        {
            runner.RunEpisode(
                brain,
                12345,
                60,
                2,
                captureSnapshots: true,
                snapshotEveryNTicks: snapshotEvery,
                snapshotsOutDir: outDir);
            string firstJson = ReadSnapshotJson(outDir, snapshotEvery);
            WorldSnapshot first = SnapshotJson.Deserialize(firstJson);

            Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            runner.RunEpisode(
                brain,
                12345,
                60,
                2,
                captureSnapshots: true,
                snapshotEveryNTicks: snapshotEvery,
                snapshotsOutDir: outDir);
            string secondJson = ReadSnapshotJson(outDir, snapshotEvery);
            WorldSnapshot second = SnapshotJson.Deserialize(secondJson);

            Assert.Equal(first.Header.WorldChecksum, second.Header.WorldChecksum);
            Assert.Equal(first.Header.AgentCount, second.Header.AgentCount);
            Assert.Equal(firstJson, secondJson);
        }
        finally
        {
            CleanupSnapshots(outDir);
        }
    }

    [Fact]
    public void Snapshot_AgentCountMatches()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new NoOpBrain();
        const int snapshotEvery = 30;
        string outDir = CreateSnapshotDirectory();

        try
        {
            runner.RunEpisode(
                brain,
                777,
                60,
                3,
                captureSnapshots: true,
                snapshotEveryNTicks: snapshotEvery,
                snapshotsOutDir: outDir);

            string json = ReadSnapshotJson(outDir, snapshotEvery);
            WorldSnapshot snapshot = SnapshotJson.Deserialize(json);

            Assert.Equal(snapshot.Header.AgentCount, snapshot.Agents.Count);
        }
        finally
        {
            CleanupSnapshots(outDir);
        }
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

    private static string CreateSnapshotDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "gaia2_snapshots", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CleanupSnapshots(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

}
