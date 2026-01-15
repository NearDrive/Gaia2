using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Core.Evo;
using Core.Sim;
using SimBrainOutput = Core.Sim.BrainOutput;

namespace App.Headless;

internal static class Program
{
    private const int DefaultTicks = 10000;
    private const int DefaultTrainTicks = 1000;
    private const int DefaultAgents = 32;
    private const float DefaultDt = 1f;
    private const int DefaultWorldWidth = 32;
    private const int DefaultWorldHeight = 32;
    private const int DefaultVisionRays = 8;
    private const float DefaultVisionRange = 10f;
    private const float DefaultVisionFov = MathF.PI / 2f;
    private const float DefaultAgentMaxSpeed = 1.5f;
    private const float DefaultMoveDeadzone = 0.05f;
    internal const string TrainLogHeader =
        "run_id,generation,population,episodes_per_genome,world_size,obstacle_density,water_proximity_bias," +
        "ticks_per_episode," +
        "best_fitness,mean_fitness,worst_fitness,best_ticks_survived,best_successful_drinks," +
        "best_avg_thirst,best_distance_traveled,best_visited_cells,best_drinkable_cells_visited," +
        "mean_visited_cells,best_nodes,best_connections";

    public static int Main(string[] args)
    {
        Options options = ParseArgs(args);

        if (options.Mode == RunMode.Compare)
        {
            RunCompare(options.ComparePathA, options.ComparePathB);
            return 0;
        }

        SimulationConfig config = new(
            options.Seed,
            DefaultDt,
            options.Ticks,
            DefaultWorldWidth,
            DefaultWorldHeight,
            DefaultVisionRays,
            DefaultVisionRange,
            DefaultVisionFov,
            DefaultAgentMaxSpeed,
            DefaultMoveDeadzone);

        if (options.Mode == RunMode.Benchmark)
        {
            RunBenchmark(options, config);
        }
        else if (options.Mode == RunMode.Replay)
        {
            RunReplay(options, config);
        }
        else if (options.Mode == RunMode.ReplayVerify)
        {
            return RunReplayVerify(options, config);
        }
        else if (options.Mode == RunMode.Sim)
        {
            Simulation simulation = new(config, options.Agents);
            simulation.Run(options.Ticks);

            ulong worldChecksum = simulation.World.ComputeChecksum();
            ulong agentsChecksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
            ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);
            (int aliveCount, float minThirst, float avgThirst, float maxThirst) = SummarizeThirst(simulation.Agents);

            Console.WriteLine($"Seed: {options.Seed}");
            Console.WriteLine($"Ticks: {options.Ticks}");
            Console.WriteLine($"Agents: {options.Agents}");
            Console.WriteLine($"Alive: {aliveCount}");
            Console.WriteLine($"Thirst01 Min/Avg/Max: {minThirst:0.000}/{avgThirst:0.000}/{maxThirst:0.000}");
            Console.WriteLine($"WorldChecksum: {worldChecksum}");
            Console.WriteLine($"AgentsChecksum: {agentsChecksum}");
            Console.WriteLine($"TotalChecksum: {totalChecksum}");
        }
        else if (options.Mode == RunMode.Episode)
        {
            EpisodeRunner runner = new(config);
            IBrain brain = CreateBrain(options.Brain);

            Console.WriteLine("Seed,TicksSurvived,SuccessfulDrinks,VisitedCells,DrinkableCellsVisited,AvgThirst01,Fitness,Checksum");

            double fitnessSum = 0.0;
            double minFitness = double.MaxValue;
            double maxFitness = double.MinValue;
            double survivalSum = 0.0;

            for (int episode = 0; episode < options.Episodes; episode += 1)
            {
                int seed = options.Seed + episode;
                EpisodeResult result = runner.RunEpisode(brain, seed, options.Ticks, 1);
                Console.WriteLine(string.Join(
                    ",",
                    result.Seed.ToString(CultureInfo.InvariantCulture),
                    result.TicksSurvived.ToString(CultureInfo.InvariantCulture),
                    result.SuccessfulDrinks.ToString(CultureInfo.InvariantCulture),
                    result.VisitedCells.ToString(CultureInfo.InvariantCulture),
                    result.DrinkableCellsVisited.ToString(CultureInfo.InvariantCulture),
                    result.AvgThirst01.ToString("0.000000", CultureInfo.InvariantCulture),
                    result.Fitness.ToString("0.000", CultureInfo.InvariantCulture),
                    result.Checksum.ToString(CultureInfo.InvariantCulture)));

                fitnessSum += result.Fitness;
                minFitness = Math.Min(minFitness, result.Fitness);
                maxFitness = Math.Max(maxFitness, result.Fitness);
                survivalSum += result.TicksSurvived / (double)result.TicksRequested;
            }

            if (options.Episodes > 1)
            {
                double meanFitness = fitnessSum / options.Episodes;
                double meanSurvival = survivalSum / options.Episodes;
                Console.WriteLine($"meanFitness={meanFitness:0.000} minFitness={minFitness:0.000} maxFitness={maxFitness:0.000} meanSurvival={meanSurvival:0.000}");
            }
        }
        else
        {
            RunTraining(options, config);
        }

        return 0;
    }

    private static Options ParseArgs(string[] args)
    {
        int? seed = null;
        int ticks = DefaultTicks;
        bool ticksSpecified = false;
        int agents = DefaultAgents;
        int episodes = 1;
        bool episodesSpecified = false;
        int generations = 20;
        int population = 64;
        bool parallel = false;
        int? maxDegree = null;
        RunMode mode = RunMode.Sim;
        string brain = "none";
        string resumePath = Path.Combine("artifacts", "checkpoint.json");
        string comparePathA = string.Empty;
        string comparePathB = string.Empty;
        string genomePath = string.Empty;
        string scenariosPath = "default";
        string outPath = string.Empty;
        string inPath = string.Empty;

        for (int i = 0; i < args.Length; i += 1)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--mode":
                    mode = ParseModeValue(args, ref i);
                    break;
                case "--seed":
                    seed = ParseIntValue(args, ref i, "--seed");
                    break;
                case "--ticks":
                    ticks = ParseIntValue(args, ref i, "--ticks");
                    ticksSpecified = true;
                    break;
                case "--agents":
                    agents = ParseIntValue(args, ref i, "--agents");
                    break;
                case "--brain":
                    brain = ParseStringValue(args, ref i, "--brain");
                    break;
                case "--episodes":
                    episodes = ParseIntValue(args, ref i, "--episodes");
                    episodesSpecified = true;
                    break;
                case "--generations":
                    generations = ParseIntValue(args, ref i, "--generations");
                    break;
                case "--population":
                    population = ParseIntValue(args, ref i, "--population");
                    break;
                case "--resume":
                    resumePath = ParseStringValue(args, ref i, "--resume");
                    break;
                case "--parallel":
                    parallel = ParseParallelValue(args, ref i);
                    break;
                case "--maxDegree":
                    maxDegree = ParseIntValue(args, ref i, "--maxDegree");
                    break;
                case "--genome":
                    genomePath = ParseStringValue(args, ref i, "--genome");
                    break;
                case "--scenarios":
                    scenariosPath = ParseStringValue(args, ref i, "--scenarios");
                    break;
                case "--out":
                    outPath = ParseStringValue(args, ref i, "--out");
                    break;
                case "--in":
                    inPath = ParseStringValue(args, ref i, "--in");
                    break;
                case "--a":
                    comparePathA = ParseStringValue(args, ref i, "--a");
                    break;
                case "--b":
                    comparePathB = ParseStringValue(args, ref i, "--b");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }
        }

        if (!seed.HasValue
            && mode != RunMode.Train
            && mode != RunMode.Compare
            && mode != RunMode.Benchmark
            && mode != RunMode.ReplayVerify)
        {
            throw new ArgumentException("Missing required --seed <int> argument.");
        }

        if (!seed.HasValue && mode == RunMode.Train)
        {
            seed = 123;
        }

        if (mode == RunMode.Train && !ticksSpecified)
        {
            ticks = DefaultTrainTicks;
        }

        if (mode == RunMode.Train && !episodesSpecified)
        {
            episodes = 3;
        }

        if (mode == RunMode.Compare && (string.IsNullOrWhiteSpace(comparePathA) || string.IsNullOrWhiteSpace(comparePathB)))
        {
            throw new ArgumentException("Missing required --a <manifestA> and --b <manifestB> arguments for compare mode.");
        }

        if (mode == RunMode.ReplayVerify && string.IsNullOrWhiteSpace(inPath))
        {
            throw new ArgumentException("Missing required --in <path> argument for replay-verify mode.");
        }

        if (ticks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        if (agents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(agents));
        }

        if (episodes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(episodes));
        }

        if (generations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generations));
        }

        if (population <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(population));
        }

        if (mode == RunMode.Episode && episodes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(episodes));
        }

        if (mode == RunMode.Episode && brain.Length == 0)
        {
            throw new ArgumentException("Missing required --brain <none|neat> argument for episode mode.");
        }

        int resolvedMaxDegree = parallel
            ? maxDegree ?? Environment.ProcessorCount
            : 0;

        if (parallel && resolvedMaxDegree <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegree));
        }

        int resolvedSeed = seed ?? 0;

        return new Options(
            resolvedSeed,
            ticks,
            agents,
            mode,
            brain,
            episodes,
            generations,
            population,
            resumePath,
            parallel,
            resolvedMaxDegree,
            comparePathA,
            comparePathB,
            genomePath,
            scenariosPath,
            inPath,
            outPath);
    }

    private static int ParseIntValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index += 1;

        if (!int.TryParse(args[index], out int value))
        {
            throw new ArgumentException($"Invalid value for {name}: '{args[index]}'.");
        }

        return value;
    }

    private static string ParseStringValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index += 1;
        return args[index];
    }

    private static RunMode ParseModeValue(string[] args, ref int index)
    {
        string value = ParseStringValue(args, ref index, "--mode");
        return value switch
        {
            "sim" => RunMode.Sim,
            "episode" => RunMode.Episode,
            "train" => RunMode.Train,
            "compare" => RunMode.Compare,
            "benchmark" => RunMode.Benchmark,
            "replay" => RunMode.Replay,
            "replay-verify" => RunMode.ReplayVerify,
            _ => throw new ArgumentException("Invalid mode '" + value + "'. Use 'sim', 'episode', 'train', 'compare', 'benchmark', 'replay', or 'replay-verify'.")
        };
    }

    private static bool ParseParallelValue(string[] args, ref int index)
    {
        int value = ParseIntValue(args, ref index, "--parallel");
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new ArgumentException($"Invalid value for --parallel: '{value}'. Use 0 or 1.")
        };
    }

    private static (int AliveCount, float MinThirst, float AvgThirst, float MaxThirst) SummarizeThirst(
        IReadOnlyList<AgentState> agents)
    {
        if (agents.Count == 0)
        {
            return (0, 0f, 0f, 0f);
        }

        int alive = 0;
        float min = float.MaxValue;
        float max = float.MinValue;
        float total = 0f;

        foreach (AgentState agent in agents)
        {
            if (agent.IsAlive)
            {
                alive += 1;
            }

            float thirst = Quantize(agent.Thirst01);
            min = Math.Min(min, thirst);
            max = Math.Max(max, thirst);
            total += thirst;
        }

        float avg = Quantize(total / agents.Count);
        return (alive, min, avg, max);
    }

    private static float Quantize(float value)
    {
        return MathF.Round(value * 1000f) / 1000f;
    }

    private static IBrain CreateBrain(string brain)
    {
        return brain switch
        {
            "none" => new NoneBrain(),
            "neat" => new HeuristicBrain(),
            _ => throw new ArgumentException($"Unknown brain '{brain}'. Use 'none' or 'neat'.")
        };
    }

    private static IBrain CreateGenomeBrain(string genomePath, SimulationConfig simConfig)
    {
        if (!File.Exists(genomePath))
        {
            throw new FileNotFoundException($"Genome file not found: {genomePath}");
        }

        string json = File.ReadAllText(genomePath);
        Genome genome = GenomeJson.Deserialize(json);

        int inputCount = (simConfig.AgentVisionRays * 3) + 2;
        int outputCount = 3;

        GenomeBrainFactory factory = new();
        return factory.CreateBrain(genome, inputCount, outputCount);
    }

    private static IBrain ResolveReplayBrain(Options options, SimulationConfig simConfig, out string brainInfo)
    {
        if (string.Equals(options.Brain, "neat", StringComparison.Ordinal))
        {
            string genomePath = string.IsNullOrWhiteSpace(options.GenomePath)
                ? Path.Combine("artifacts", "best.json")
                : options.GenomePath;
            brainInfo = BuildBrainInfo(genomePath);
            return CreateGenomeBrain(genomePath, simConfig);
        }

        if (string.Equals(options.Brain, "none", StringComparison.Ordinal))
        {
            brainInfo = "none";
            return new NoneBrain();
        }

        throw new ArgumentException($"Unknown brain '{options.Brain}'. Use 'none' or 'neat'.");
    }

    private static CurriculumSchedule CreateDefaultCurriculum(Options options, SimulationConfig simConfig)
    {
        int baseWorldSize = Math.Max(simConfig.WorldWidth, simConfig.WorldHeight);
        int startWorldSize = Math.Max(8, baseWorldSize - 8);
        int endWorldSize = baseWorldSize + 8;

        CurriculumPhase start = new(
            WaterProximityBias01: 1f,
            ObstacleDensity01: 0.1f,
            WorldSize: startWorldSize,
            TicksPerEpisode: options.Ticks,
            ThirstRatePerSecond: simConfig.ThirstRatePerSecond);

        CurriculumPhase end = new(
            WaterProximityBias01: 0f,
            ObstacleDensity01: 0.4f,
            WorldSize: endWorldSize,
            TicksPerEpisode: options.Ticks,
            ThirstRatePerSecond: simConfig.ThirstRatePerSecond * 1.2f);

        int totalGenerations = Math.Max(1, options.Generations);
        return new CurriculumSchedule(totalGenerations, start, end);
    }

    internal static void RunTraining(Options options, SimulationConfig simConfig)
    {
        Directory.CreateDirectory("artifacts");
        string logPath = Path.Combine("artifacts", "train_log.csv");

        EvolutionConfig evoConfig;
        Population population;
        EvoRng rng;

        if (File.Exists(options.ResumePath))
        {
            PopulationSnapshot snapshot = LoadCheckpoint(options.ResumePath);
            evoConfig = snapshot.Config;
            population = snapshot.Population;
            rng = new EvoRng(snapshot.RngState);
            Console.WriteLine($"Resuming from gen {population.Generation}");
        }
        else
        {
            evoConfig = new EvolutionConfig(
                options.Population,
                eliteCount: 4,
                mutationRate: 0.8,
                addConnectionRate: 0.1,
                addNodeRate: 0.03,
                weightPerturbRate: 0.8,
                weightResetRate: 0.1,
                maxNodes: 64,
                maxConnections: 256);

            Evolver freshEvolver = new(evoConfig);
            int inputCount = (simConfig.AgentVisionRays * 3) + 2;
            int outputCount = 3;
            population = freshEvolver.CreateInitialPopulation(
                options.Seed,
                options.Population,
                inputCount,
                outputCount);
            rng = new EvoRng(options.Seed);
            Console.WriteLine("Starting new run");
        }

        Evolver evolver = new(evoConfig);
        evolver.RestoreStateFromPopulation(population);

        CurriculumSchedule curriculum = CreateDefaultCurriculum(options, simConfig);
        Trainer trainer = new();
        Genome? bestOverall = null;
        GenomeFitnessScore? bestOverallScore = null;
        double? initialBestFitness = null;
        double? finalBestFitness = null;
        string? bestOverallPath = null;
        GenerationResult? finalResult = null;

        using StreamWriter logWriter = CreateTrainLogWriter(logPath);

        while (population.Generation < options.Generations)
        {
            int gen = population.Generation;
            int baseSeed = options.Seed + (gen * 10000);
            GenerationResult result = trainer.RunGeneration(
                population,
                baseSeed,
                options.Episodes,
                simConfig,
                curriculum,
                options.Parallel,
                options.Parallel ? options.MaxDegree : null);

            Genome? bestGenome = result.BestGenome;
            if (bestGenome is not null)
            {
                GenomeFitnessScore generationScore = new(
                    result.BestGenomeIndex,
                    result.BestFitness,
                    result.RawFitnesses[result.BestGenomeIndex],
                    result.BestGenomeNodeCount,
                    result.BestGenomeConnectionCount);

                if (!bestOverallScore.HasValue || GenomeFitnessScore.Compare(generationScore, bestOverallScore.Value) < 0)
                {
                    bestOverallScore = generationScore;
                    bestOverall = bestGenome;
                }
            }

            if (!initialBestFitness.HasValue)
            {
                initialBestFitness = result.BestFitness;
            }

            finalBestFitness = result.BestFitness;
            finalResult = result;

            Console.WriteLine(
                $"Gen={result.Generation} Best={result.BestFitness:0.000} Mean={result.MeanFitness:0.000} " +
                $"Worst={result.WorstFitness:0.000} Nodes={bestGenome?.NodeCount ?? 0} Conns={bestGenome?.ConnectionCount ?? 0}");

            WriteTrainLogLine(
                logWriter,
                runId: $"seed_{options.Seed}",
                result,
                population.Genomes.Count,
                options.Episodes,
                result.TicksPerEpisode);

            if (bestGenome is not null)
            {
                string generationPath = Path.Combine("artifacts", $"best_gen_{result.Generation:000}.json");
                WriteGenomeJson(bestGenome, generationPath);
            }

            population = evolver.NextGeneration(population, result.Fitnesses, result.RawFitnesses, rng);
            WriteCheckpoint(population, evoConfig, rng.GetState(), options.ResumePath);
        }

        if (bestOverall is not null)
        {
            bestOverallPath = Path.Combine("artifacts", "best.json");
            WriteGenomeJson(bestOverall, bestOverallPath);
        }

        if (initialBestFitness.HasValue && finalBestFitness.HasValue)
        {
            double delta = finalBestFitness.Value - initialBestFitness.Value;
            Console.WriteLine($"BestFitness Initial={initialBestFitness.Value:0.000} Final={finalBestFitness.Value:0.000} Delta={delta:0.000}");
        }

        Console.WriteLine($"BestGenome Path={bestOverallPath ?? "none"}");
        Console.WriteLine($"TrainLog Path={logPath}");

        if (finalResult is not null)
        {
            WriteRunManifest(options, evoConfig, logPath, bestOverallPath, finalResult, curriculum);
        }
    }

    private static void WriteGenomeJson(Genome genome, string path)
    {
        string json = GenomeJson.Serialize(genome);
        File.WriteAllText(path, json);
    }

    private static void WriteCheckpoint(Population population, EvolutionConfig config, RngState rngState, string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = PopulationJson.Serialize(population, config, rngState);
        File.WriteAllText(path, json);
    }

    private static PopulationSnapshot LoadCheckpoint(string path)
    {
        string json = File.ReadAllText(path);
        return PopulationJson.Deserialize(json);
    }

    internal static ManifestComparisonResult CompareManifests(string pathA, string pathB)
    {
        RunManifest manifestA = RunManifestJson.Read(pathA);
        RunManifest manifestB = RunManifestJson.Read(pathB);
        IReadOnlyList<string> differences = RunManifestComparer.Compare(manifestA, manifestB);
        return new ManifestComparisonResult(differences.Count == 0, differences);
    }

    internal static void RunCompare(string pathA, string pathB)
    {
        ManifestComparisonResult result = CompareManifests(pathA, pathB);
        Console.WriteLine($"Equivalent: {(result.Equivalent ? "YES" : "NO")}");
        if (!result.Equivalent)
        {
            Console.WriteLine("Differences:");
            foreach (string difference in result.Differences)
            {
                Console.WriteLine($"- {difference}");
            }
        }
    }

    private static void WriteRunManifest(
        Options options,
        EvolutionConfig evoConfig,
        string logPath,
        string? bestGenomePath,
        GenerationResult finalResult,
        CurriculumSchedule curriculum)
    {
        CurriculumPhase startPhase = curriculum.GetPhase(0);
        CurriculumPhase endPhase = curriculum.GetPhase(curriculum.TotalGenerations - 1);
        RunManifest manifest = new(
            SchemaVersion: 2,
            CreatedUtc: DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Seed: options.Seed,
            Generations: options.Generations,
            Population: options.Population,
            EpisodesPerGenome: options.Episodes,
            TicksPerEpisode: options.Ticks,
            Parallel: options.Parallel ? 1 : 0,
            MaxDegree: options.MaxDegree,
            EvolutionConfig: new EvolutionConfigManifest(
                evoConfig.EliteCount,
                evoConfig.MutationRate,
                evoConfig.AddConnectionRate,
                evoConfig.AddNodeRate,
                evoConfig.WeightPerturbRate,
                evoConfig.WeightResetRate,
                evoConfig.MaxNodes,
                evoConfig.MaxConnections),
            CurriculumSchedule: new CurriculumScheduleManifest(
                curriculum.TotalGenerations,
                Start: new CurriculumPhaseManifest(
                    startPhase.WaterProximityBias01,
                    startPhase.ObstacleDensity01,
                    startPhase.WorldSize,
                    startPhase.TicksPerEpisode,
                    startPhase.ThirstRatePerSecond),
                End: new CurriculumPhaseManifest(
                    endPhase.WaterProximityBias01,
                    endPhase.ObstacleDensity01,
                    endPhase.WorldSize,
                    endPhase.TicksPerEpisode,
                    endPhase.ThirstRatePerSecond)),
            BestGenomePath: bestGenomePath ?? string.Empty,
            CheckpointPath: options.ResumePath,
            TrainLogPath: logPath,
            FinalSummary: new RunManifestSummary(
                finalResult.BestFitness,
                finalResult.MeanFitness,
                finalResult.BestGenomeNodeCount,
                finalResult.BestGenomeConnectionCount,
                finalResult.BestTicksSurvived,
                finalResult.BestSuccessfulDrinks,
                finalResult.BestAvgThirst01));

        string manifestPath = Path.Combine("artifacts", "run_manifest.json");
        RunManifestJson.Write(manifestPath, manifest);
    }

    private static IReadOnlyList<Scenario> ResolveScenarios(string scenariosPath)
    {
        if (string.Equals(scenariosPath, "default", StringComparison.OrdinalIgnoreCase))
        {
            return BenchmarkScenarios.Default;
        }

        if (!File.Exists(scenariosPath))
        {
            throw new FileNotFoundException($"Scenario file not found: {scenariosPath}");
        }

        return ScenarioJson.Read(scenariosPath);
    }

    private static IBrain ResolveBenchmarkBrain(Options options, SimulationConfig simConfig)
    {
        if (!string.Equals(options.Brain, "none", StringComparison.Ordinal))
        {
            return CreateBrain(options.Brain);
        }

        string genomePath = string.IsNullOrWhiteSpace(options.GenomePath)
            ? Path.Combine("artifacts", "best.json")
            : options.GenomePath;

        return CreateGenomeBrain(genomePath, simConfig);
    }

    internal static void RunBenchmark(Options options, SimulationConfig simConfig)
    {
        Directory.CreateDirectory("artifacts");

        IReadOnlyList<Scenario> scenarios = ResolveScenarios(options.ScenariosPath);
        IBrain brain = ResolveBenchmarkBrain(options, simConfig);

        BenchmarkRunner runner = new(simConfig);
        BenchmarkReport report = runner.Run(brain, scenarios);

        string reportPath = Path.Combine("artifacts", "benchmark_report.json");
        BenchmarkReportJson.Write(reportPath, report);

        string csvPath = Path.Combine("artifacts", "benchmark.csv");
        BenchmarkCsvWriter.Write(csvPath, report);
    }

    internal static void RunReplay(Options options, SimulationConfig simConfig)
    {
        EpisodeRunner runner = new(simConfig);
        IBrain brain = ResolveReplayBrain(options, simConfig, out string brainInfo);

        EpisodeResult result = runner.RunEpisode(
            brain,
            options.Seed,
            options.Ticks,
            agentCount: 1,
            captureReplay: true,
            scenario: null,
            brainInfo: brainInfo);

        Replay replay = runner.LastReplay ?? throw new InvalidOperationException("Replay capture failed.");
        string outputPath = string.IsNullOrWhiteSpace(options.OutPath)
            ? Path.Combine("artifacts", "replays", $"replay_seed{options.Seed}.json")
            : options.OutPath;
        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        ReplayJson.Write(outputPath, replay);

        Console.WriteLine($"Replay saved: {outputPath}");
        Console.WriteLine($"TicksSurvived: {result.TicksSurvived}");
        Console.WriteLine($"FinalChecksum: {replay.FinalChecksum}");
    }

    internal static int RunReplayVerify(Options options, SimulationConfig simConfig)
    {
        Replay replay = ReplayJson.Read(options.InPath);
        ReplayVerificationResult result = ReplayVerifier.Verify(replay, simConfig);

        if (result.Success)
        {
            Console.WriteLine("VERIFY OK");
            return 0;
        }

        Console.WriteLine("VERIFY FAIL");
        Console.WriteLine($"ExpectedChecksum: {result.ExpectedChecksum}");
        Console.WriteLine($"ComputedChecksum: {result.ComputedChecksum}");
        Console.WriteLine($"ExpectedWorldChecksum: {result.ExpectedWorldChecksum}");
        Console.WriteLine($"ComputedWorldChecksum: {result.ComputedWorldChecksum}");
        Console.WriteLine($"TicksSimulated: {result.TicksSimulated}");
        return 1;
    }

    internal enum RunMode
    {
        Sim,
        Episode,
        Train,
        Compare,
        Benchmark,
        Replay,
        ReplayVerify
    }

    internal readonly record struct Options(
        int Seed,
        int Ticks,
        int Agents,
        RunMode Mode,
        string Brain,
        int Episodes,
        int Generations,
        int Population,
        string ResumePath,
        bool Parallel,
        int MaxDegree,
        string ComparePathA,
        string ComparePathB,
        string GenomePath,
        string ScenariosPath,
        string InPath,
        string OutPath = "");

    private static string BuildBrainInfo(string genomePath)
    {
        FileInfo info = new(genomePath);
        using FileStream stream = File.OpenRead(genomePath);
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(stream);
        string hashHex = Convert.ToHexString(hash).ToLowerInvariant();
        string fileName = Path.GetFileName(genomePath);
        return $"{fileName} sha256={hashHex} size={info.Length}";
    }

    internal readonly record struct ManifestComparisonResult(bool Equivalent, IReadOnlyList<string> Differences);

    private static StreamWriter CreateTrainLogWriter(string logPath)
    {
        bool writeHeader = !File.Exists(logPath);
        FileStream stream = new(
            logPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read);
        StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (writeHeader)
        {
            writer.WriteLine(TrainLogHeader);
            writer.Flush();
        }

        return writer;
    }

    private static void WriteTrainLogLine(
        StreamWriter writer,
        string runId,
        GenerationResult result,
        int population,
        int episodesPerGenome,
        int ticksPerEpisode)
    {
        float quantizedAvgThirst = Quantize(result.BestAvgThirst01);
        string line = string.Join(
            ",",
            runId,
            result.Generation.ToString(CultureInfo.InvariantCulture),
            population.ToString(CultureInfo.InvariantCulture),
            episodesPerGenome.ToString(CultureInfo.InvariantCulture),
            result.WorldSize.ToString(CultureInfo.InvariantCulture),
            result.ObstacleDensity01.ToString("0.000", CultureInfo.InvariantCulture),
            result.WaterProximityBias01.ToString("0.000", CultureInfo.InvariantCulture),
            ticksPerEpisode.ToString(CultureInfo.InvariantCulture),
            result.BestFitness.ToString("0.000", CultureInfo.InvariantCulture),
            result.MeanFitness.ToString("0.000", CultureInfo.InvariantCulture),
            result.WorstFitness.ToString("0.000", CultureInfo.InvariantCulture),
            result.BestTicksSurvived.ToString(CultureInfo.InvariantCulture),
            result.BestSuccessfulDrinks.ToString(CultureInfo.InvariantCulture),
            quantizedAvgThirst.ToString("0.000", CultureInfo.InvariantCulture),
            result.BestDistanceTraveled.ToString("0.000", CultureInfo.InvariantCulture),
            result.BestVisitedCells.ToString(CultureInfo.InvariantCulture),
            result.BestDrinkableCellsVisited.ToString(CultureInfo.InvariantCulture),
            result.MeanVisitedCells.ToString("0.000", CultureInfo.InvariantCulture),
            result.BestGenomeNodeCount.ToString(CultureInfo.InvariantCulture),
            result.BestGenomeConnectionCount.ToString(CultureInfo.InvariantCulture));

        writer.WriteLine(line);
        writer.Flush();
    }

    private sealed class NoneBrain : IBrain
    {
        public SimBrainOutput DecideAction(BrainInput input)
        {
            return default;
        }
    }

    private sealed class HeuristicBrain : IBrain
    {
        public SimBrainOutput DecideAction(BrainInput input)
        {
            float drinkScore = input.Thirst01 > 0.6f ? 1f : 0f;
            return new SimBrainOutput(0.5f, 0f, drinkScore);
        }
    }

}
