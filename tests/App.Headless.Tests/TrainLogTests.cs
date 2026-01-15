using App.Headless;
using Core.Sim;
using Xunit;

namespace App.Headless.Tests;

[Collection("AppHeadlessTraining")]
public class TrainLogTests
{
    [Fact]
    public void TrainLog_WritesHeaderAndRows()
    {
        string tempDir = CreateTempDir();
        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            RunTraining(seed: 123, generations: 3, population: 5, episodes: 1, ticks: 20);

            string logPath = Path.Combine(tempDir, "artifacts", "train_log.csv");
            Assert.True(File.Exists(logPath));

            string[] lines = File.ReadAllLines(logPath);
            Assert.Equal(Program.TrainLogHeader, lines[0]);
            Assert.Equal(4, lines.Length);

            int expectedColumns = Program.TrainLogHeader.Split(',').Length;
            foreach (string line in lines)
            {
                Assert.Equal(expectedColumns, line.Split(',').Length);
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Metrics_AreDeterministic_ForSameSeed()
    {
        byte[] firstRun = RunTrainingInTempDir(seed: 42, generations: 3, population: 5, episodes: 1, ticks: 20);
        byte[] secondRun = RunTrainingInTempDir(seed: 42, generations: 3, population: 5, episodes: 1, ticks: 20);

        Assert.Equal(firstRun, secondRun);
    }

    private static byte[] RunTrainingInTempDir(int seed, int generations, int population, int episodes, int ticks)
    {
        string tempDir = CreateTempDir();
        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            RunTraining(seed, generations, population, episodes, ticks);

            string logPath = Path.Combine(tempDir, "artifacts", "train_log.csv");
            return File.ReadAllBytes(logPath);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void RunTraining(int seed, int generations, int population, int episodes, int ticks)
    {
        SimulationConfig simConfig = new(
            Seed: seed,
            Dt: 1f,
            TicksPerEpisode: ticks,
            WorldWidth: 16,
            WorldHeight: 16,
            AgentVisionRays: 4,
            AgentVisionRange: 6f,
            AgentFov: MathF.PI / 2f);

        Program.Options options = new(
            Seed: seed,
            Ticks: ticks,
            Agents: 1,
            Mode: Program.RunMode.Train,
            Brain: "neat",
            Episodes: episodes,
            Generations: generations,
            Population: population,
            ResumePath: Path.Combine("artifacts", "checkpoint.json"));

        Program.RunTraining(options, simConfig);
    }

    private static string CreateTempDir()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"gaia2_train_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}

[CollectionDefinition("AppHeadlessTraining", DisableParallelization = true)]
public class AppHeadlessTrainingCollection;
