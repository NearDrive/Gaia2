using App.Headless;
using Core.Evo;
using Core.Sim;
using Xunit;

namespace App.Headless.Tests;

[Collection("AppHeadlessTraining")]
public class TrainLogTests
{
    private const int MiniTicks = 10;
    private const int MiniPopulation = 4;
    private const int MiniGenerations = 2;
    private const int MiniEpisodes = 1;

    [Fact]
    public void TrainLog_WritesHeaderAndRows()
    {
        string tempDir = CreateTempDir();
        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            RunTraining(seed: 123, generations: MiniGenerations, population: MiniPopulation, episodes: MiniEpisodes, ticks: MiniTicks);

            string logPath = Path.Combine(tempDir, "artifacts", "train_log.csv");
            Assert.True(File.Exists(logPath));

            string[] lines = File.ReadAllLines(logPath);
            Assert.Equal(Program.TrainLogHeader, lines[0]);
            Assert.Equal(MiniGenerations + 1, lines.Length);

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
    public void ParallelOff_MatchesExistingDeterminism()
    {
        byte[] firstRun = RunTrainingInTempDir(
            seed: 42,
            generations: MiniGenerations,
            population: MiniPopulation,
            episodes: MiniEpisodes,
            ticks: MiniTicks,
            parallel: false,
            maxDegree: 0);
        byte[] secondRun = RunTrainingInTempDir(
            seed: 42,
            generations: MiniGenerations,
            population: MiniPopulation,
            episodes: MiniEpisodes,
            ticks: MiniTicks,
            parallel: false,
            maxDegree: 0);

        Assert.Equal(firstRun, secondRun);
    }

    [Fact]
    public void ParallelOn_EqualsParallelOff_Fitnesses()
    {
        int seed = 101;
        SimulationConfig simConfig = CreateMiniSimConfig(seed, MiniTicks);
        EvolutionConfig evoConfig = CreateMiniEvolutionConfig();

        int inputCount = (simConfig.AgentVisionRays * 3) + 1;
        int outputCount = 1;

        Evolver evolver = new(evoConfig);
        Population population = evolver.CreateInitialPopulation(seed, evoConfig.PopulationSize, inputCount, outputCount);
        Trainer trainer = new();

        GenerationResult sequential = trainer.RunGeneration(
            population,
            baseSeed: seed + 1000,
            episodesPerGenome: MiniEpisodes,
            simConfig,
            ticksPerEpisode: MiniTicks,
            useParallel: false);

        GenerationResult parallel = trainer.RunGeneration(
            population,
            baseSeed: seed + 1000,
            episodesPerGenome: MiniEpisodes,
            simConfig,
            ticksPerEpisode: MiniTicks,
            useParallel: true,
            maxDegreeOfParallelism: 2);

        Assert.Equal(sequential.BestFitness, parallel.BestFitness);
        Assert.Equal(sequential.MeanFitness, parallel.MeanFitness);
        Assert.Equal(sequential.WorstFitness, parallel.WorstFitness);
        Assert.Equal(sequential.BestGenomeIndex, parallel.BestGenomeIndex);
        Assert.Equal(sequential.Fitnesses, parallel.Fitnesses);
        Assert.Equal(sequential.RawFitnesses, parallel.RawFitnesses);
    }

    private static byte[] RunTrainingInTempDir(
        int seed,
        int generations,
        int population,
        int episodes,
        int ticks,
        bool parallel,
        int maxDegree)
    {
        string tempDir = CreateTempDir();
        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            RunTraining(seed, generations, population, episodes, ticks, parallel, maxDegree);

            string logPath = Path.Combine(tempDir, "artifacts", "train_log.csv");
            return File.ReadAllBytes(logPath);
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
        int ticks,
        bool parallel = false,
        int maxDegree = 0)
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
            Parallel: parallel,
            MaxDegree: maxDegree,
            ComparePathA: string.Empty,
            ComparePathB: string.Empty);

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

    private static EvolutionConfig CreateMiniEvolutionConfig()
    {
        return new EvolutionConfig(
            populationSize: MiniPopulation,
            eliteCount: 2,
            mutationRate: 0.8,
            addConnectionRate: 0.1,
            addNodeRate: 0.02,
            weightPerturbRate: 0.8,
            weightResetRate: 0.1,
            maxNodes: 16,
            maxConnections: 32);
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
