using App.Headless;
using Core.Evo;
using Core.Sim;
using Xunit;

namespace App.Headless.Tests;

[Collection("AppHeadlessTraining")]
public class RunManifestTests
{
    private const int MiniTicks = 10;
    private const int MiniPopulation = 4;
    private const int MiniGenerations = 2;
    private const int MiniEpisodes = 1;

    [Fact]
    public void Manifest_Written_And_Stable_ForSameRun()
    {
        RunManifest first = RunTrainingAndLoadManifest(seed: 321);
        RunManifest second = RunTrainingAndLoadManifest(seed: 321);

        IReadOnlyList<string> differences = RunManifestComparer.Compare(first, second);

        Assert.Empty(differences);
    }

    [Fact]
    public void CompareMode_DetectsDifference()
    {
        string tempDir = CreateTempDir();
        try
        {
            string pathA = Path.Combine(tempDir, "a.json");
            string pathB = Path.Combine(tempDir, "b.json");

            RunManifest baseManifest = CreateSampleManifest(population: 4);
            RunManifest modifiedManifest = CreateSampleManifest(population: 5);

            RunManifestJson.Write(pathA, baseManifest);
            RunManifestJson.Write(pathB, modifiedManifest);

            Program.ManifestComparisonResult result = Program.CompareManifests(pathA, pathB);

            Assert.False(result.Equivalent);
            Assert.Contains("population", result.Differences);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static RunManifest RunTrainingAndLoadManifest(int seed)
    {
        string tempDir = CreateTempDir();
        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            RunTraining(seed, MiniGenerations, MiniPopulation, MiniEpisodes, MiniTicks);

            string manifestPath = Path.Combine(tempDir, "artifacts", "run_manifest.json");
            Assert.True(File.Exists(manifestPath));
            return RunManifestJson.Read(manifestPath);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void RunTraining(
        int seed,
        int generations,
        int population,
        int episodes,
        int ticks)
    {
        SimulationConfig simConfig = CreateMiniSimConfig(seed, ticks);

        Program.Options options = new(
            Seed: seed,
            Ticks: ticks,
            Agents: 1,
            Mode: Program.RunMode.Train,
            Brain: "neat",
            Episodes: episodes,
            Generations: generations,
            Population: population,
            ResumePath: Path.Combine("artifacts", "checkpoint.json"),
            Parallel: false,
            MaxDegree: 0,
            ComparePathA: string.Empty,
            ComparePathB: string.Empty,
            GenomePath: string.Empty,
            ScenariosPath: "default");

        Program.RunTraining(options, simConfig);
    }

    private static SimulationConfig CreateMiniSimConfig(int seed, int ticks)
    {
        return new SimulationConfig(
            Seed: seed,
            Dt: 1f,
            TicksPerEpisode: ticks,
            WorldWidth: 12,
            WorldHeight: 12,
            AgentVisionRays: 4,
            AgentVisionRange: 5f,
            AgentFov: MathF.PI / 2f);
    }

    private static RunManifest CreateSampleManifest(int population)
    {
        return new RunManifest(
            SchemaVersion: 2,
            CreatedUtc: "2024-01-01T00:00:00Z",
            Seed: 123,
            Generations: 2,
            Population: population,
            EpisodesPerGenome: 1,
            TicksPerEpisode: 10,
            Parallel: 0,
            MaxDegree: 0,
            EvolutionConfig: new EvolutionConfigManifest(
                EliteCount: 2,
                MutationRate: 0.8,
                AddConnectionRate: 0.1,
                AddNodeRate: 0.02,
                WeightPerturbRate: 0.8,
                WeightResetRate: 0.1,
                MaxNodes: 16,
                MaxConnections: 32),
            CurriculumSchedule: new CurriculumScheduleManifest(
                TotalGenerations: 2,
                Start: new CurriculumPhaseManifest(
                    WaterProximityBias01: 1f,
                    ObstacleDensity01: 0.1f,
                    WorldSize: 12,
                    TicksPerEpisode: 10,
                    ThirstRatePerSecond: 0.01f),
                End: new CurriculumPhaseManifest(
                    WaterProximityBias01: 0f,
                    ObstacleDensity01: 0.4f,
                    WorldSize: 16,
                    TicksPerEpisode: 10,
                    ThirstRatePerSecond: 0.012f)),
            BestGenomePath: "artifacts/best.json",
            CheckpointPath: "artifacts/checkpoint.json",
            TrainLogPath: "artifacts/train_log.csv",
            FinalSummary: new RunManifestSummary(
                BestFitness: 1.0,
                MeanFitness: 0.5,
                BestGenomeNodes: 5,
                BestGenomeConnections: 8,
                BestTicksSurvived: 9,
                BestSuccessfulDrinks: 1,
                BestAvgThirst01: 0.25f));
    }

    private static string CreateTempDir()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"gaia2_manifest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
