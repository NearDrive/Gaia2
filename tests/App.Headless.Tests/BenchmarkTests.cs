using App.Headless;
using Core.Sim;
using Xunit;

namespace App.Headless.Tests;

public class BenchmarkTests
{
    [Fact]
    public void Benchmark_CreatesOutputs()
    {
        string tempDir = CreateTempDir();
        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            string scenarioPath = Path.Combine(tempDir, "scenarios.json");
            File.WriteAllText(scenarioPath, """
                [
                  {
                    "name": "tiny",
                    "seed": 5,
                    "worldSize": 8,
                    "obstacleDensity01": 0.1,
                    "waterProximityBias01": 0.4,
                    "ticks": 20,
                    "thirstRatePerSecond": 0.01
                  }
                ]
                """);

            SimulationConfig simConfig = new(
                Seed: 1,
                Dt: 1f,
                TicksPerEpisode: 20,
                WorldWidth: 8,
                WorldHeight: 8,
                AgentVisionRays: 4,
                AgentVisionRange: 5f,
                AgentFov: MathF.PI / 2f);

            Program.Options options = new(
                Seed: 1,
                Ticks: 20,
                Agents: 1,
                Mode: Program.RunMode.Benchmark,
                Brain: "neat",
                Episodes: 1,
                Generations: 1,
                Population: 1,
                ResumePath: Path.Combine("artifacts", "checkpoint.json"),
                Parallel: false,
                MaxDegree: 0,
                ComparePathA: string.Empty,
                ComparePathB: string.Empty,
                GenomePath: string.Empty,
                ScenariosPath: scenarioPath);

            Program.RunBenchmark(options, simConfig);

            string reportPath = Path.Combine(tempDir, "artifacts", "benchmark_report.json");
            string csvPath = Path.Combine(tempDir, "artifacts", "benchmark.csv");

            Assert.True(File.Exists(reportPath));
            Assert.True(File.Exists(csvPath));
            Assert.NotEqual(0, new FileInfo(reportPath).Length);
            Assert.NotEqual(0, new FileInfo(csvPath).Length);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"gaia2_benchmark_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
