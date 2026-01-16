using System.Globalization;
using System.Text;
using System.Text.Json;
using Core.Evo;
using Core.Sim;
using Persistence.Metrics;

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
    private const int DefaultEmbeddingDimension = 8;
    private const float DefaultAgentMaxSpeed = 1.5f;
    private const float DefaultAgentTurnRateRad = MathF.PI / 4f;
    private const float DefaultMoveDeadzone = 0.05f;
    internal const string TrainLogHeader =
        "run_id,generation,population,episodes_per_genome,world_size,obstacle_density,water_proximity_bias," +
        "ticks_per_episode," +
        "best_fitness,mean_fitness,worst_fitness,best_ticks_survived,best_successful_drinks," +
        "best_avg_thirst,best_distance_traveled,best_visited_cells,best_drinkable_cells_visited," +
        "mean_visited_cells,best_nodes,best_connections";

    public static int Main(string[] args)
    {
        Options options = NormalizeOptions(ParseArgs(args));

        if (options.Snapshots && options.SnapshotEvery <= 0)
        {
            Console.Error.WriteLine("Missing required --snapshot-every <int> argument when --snapshots is enabled.");
            return 1;
        }

        if (options.Mode == RunMode.Compare)
        {
            RunCompare(options.ComparePathA, options.ComparePathB);
            return 0;
        }

        if (ShouldPrepareEmbeddings(options.Mode))
        {
            EnsureEmbeddingRegistry(options);
        }

        SimulationConfig config = new(
            seed: options.Seed,
            dt: DefaultDt,
            ticksPerEpisode: options.Ticks,
            worldWidth: DefaultWorldWidth,
            worldHeight: DefaultWorldHeight,
            agentVisionRays: DefaultVisionRays,
            agentVisionRange: DefaultVisionRange,
            agentFov: DefaultVisionFov,
            agentMaxSpeed: DefaultAgentMaxSpeed,
            agentTurnRateRad: DefaultAgentTurnRateRad,
            moveDeadzone: DefaultMoveDeadzone,
            embeddingDimension: options.EmbeddingDimension,
            embeddingSeed: options.EmbeddingSeed);

        if (options.Mode == RunMode.Replay)
        {
            ReplayRecord replay = CreateReplayRecord(options, config);
            ReplayJson.Write(options.ReplayOutputPath, replay);
            return 0;
        }

        if (options.Mode == RunMode.ReplayVerify)
        {
            ReplayVerificationResult verification = VerifyReplay(options.ReplayInputPath);
            if (verification.Verified)
            {
                Console.WriteLine("VERIFY OK");
                return 0;
            }

            Console.WriteLine($"VERIFY FAIL expected={verification.ExpectedChecksum} actual={verification.ActualChecksum}");
            return 2;
        }

        if (options.Mode == RunMode.Benchmark)
        {
            RunBenchmark(options, config);
            WriteOutputManifest(options);
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
            WriteOutputManifest(options);
        }
        else if (options.Mode == RunMode.Episode)
        {
            EpisodeRunner runner = new(config);
            IBrain brain = CreateBrain(options.Brain);
            List<RunMetricsRow>? metricsRows = options.Metrics ? new List<RunMetricsRow>() : null;

            Console.WriteLine("Seed,TicksSurvived,SuccessfulDrinks,VisitedCells,DrinkableCellsVisited,AvgThirst01,Fitness,Checksum");

            double fitnessSum = 0.0;
            double minFitness = double.MaxValue;
            double maxFitness = double.MinValue;
            double survivalSum = 0.0;

            for (int episode = 0; episode < options.Episodes; episode += 1)
            {
                int seed = options.Seed + episode;
                EpisodeResult result = runner.RunEpisode(
                    brain,
                    seed,
                    options.Ticks,
                    1,
                    options.Snapshots,
                    options.SnapshotEvery,
                    options.SnapshotDirectory,
                    writeSnapshotsToDisk: !options.Snapshots);

                if (options.Snapshots)
                {
                    string snapshotStreamPath = options.SnapshotStreamPath
                        ?? throw new InvalidOperationException("Snapshot stream path was not resolved.");
                    SnapshotStreamHeader header = new()
                    {
                        Seed = seed,
                        WorldWidth = config.WorldWidth,
                        WorldHeight = config.WorldHeight,
                        Dt = config.Dt,
                        SnapshotEveryNTicks = options.SnapshotEvery
                    };
                    WriteSnapshotStream(snapshotStreamPath, header, result.Snapshots);
                }

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

                if (metricsRows is not null)
                {
                    double[] fitness = { result.Fitness };
                    metricsRows.Add(new RunMetricsRow
                    {
                        Index = episode,
                        BestFitness = (float)result.Fitness,
                        AvgFitness = (float)result.Fitness,
                        P50Fitness = MetricsStatistics.ComputePercentile(fitness, 0.50),
                        P90Fitness = MetricsStatistics.ComputePercentile(fitness, 0.90),
                        AliveAtEnd = result.TicksSurvived >= result.TicksRequested ? 1 : 0,
                        FinalChecksum = result.Checksum
                    });
                }

                fitnessSum += result.Fitness;
                minFitness = Math.Min(minFitness, result.Fitness);
                maxFitness = Math.Max(maxFitness, result.Fitness);
                survivalSum += result.TicksSurvived / (double)result.TicksRequested;
            }

            if (metricsRows is not null)
            {
                string metricsOut = options.MetricsOut
                    ?? throw new InvalidOperationException("Metrics output path was not resolved.");
                CsvMetricsWriter.Write(metricsOut, metricsRows);
            }

            if (options.Episodes > 1)
            {
                double meanFitness = fitnessSum / options.Episodes;
                double meanSurvival = survivalSum / options.Episodes;
                Console.WriteLine($"meanFitness={meanFitness:0.000} minFitness={minFitness:0.000} maxFitness={maxFitness:0.000} meanSurvival={meanSurvival:0.000}");
            }

            WriteOutputManifest(options);
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
        string outDir = string.Empty;
        string genomePath = string.Empty;
        string scenariosPath = "default";
        bool snapshots = false;
        int? snapshotEvery = null;
        string? snapshotStreamPath = null;
        bool metrics = false;
        string? metricsOut = null;
        string replayInputPath = string.Empty;
        string replayOutputPath = string.Empty;
        string embeddingsPath = string.Empty;
        int embeddingDimension = 8;
        int? embeddingSeed = null;

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
                case "--out-dir":
                    outDir = ParseStringValue(args, ref i, "--out-dir");
                    break;
                case "--embeddings":
                    embeddingsPath = ParseStringValue(args, ref i, "--embeddings");
                    break;
                case "--embedding-dim":
                    embeddingDimension = ParseIntValue(args, ref i, "--embedding-dim");
                    break;
                case "--embedding-seed":
                    embeddingSeed = ParseIntValue(args, ref i, "--embedding-seed");
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
                case "--snapshots":
                    snapshots = true;
                    break;
                case "--snapshot-every":
                    snapshotEvery = ParseIntValue(args, ref i, "--snapshot-every");
                    break;
                case "--snapshot-stream":
                    snapshotStreamPath = ParseStringValue(args, ref i, "--snapshot-stream");
                    break;
                case "--metrics":
                    metrics = true;
                    break;
                case "--metrics-out":
                    metricsOut = ParseStringValue(args, ref i, "--metrics-out");
                    break;
                case "--in":
                    replayInputPath = ParseStringValue(args, ref i, "--in");
                    break;
                case "--out":
                    replayOutputPath = ParseStringValue(args, ref i, "--out");
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

        if (mode == RunMode.Replay && string.IsNullOrWhiteSpace(replayOutputPath))
        {
            throw new ArgumentException("Missing required --out <path> argument for replay mode.");
        }

        if (mode == RunMode.ReplayVerify && string.IsNullOrWhiteSpace(replayInputPath))
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

        if (snapshotEvery.HasValue && snapshotEvery.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshotEvery), "Snapshot interval must be > 0.");
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

        if (embeddingDimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(embeddingDimension), "Embedding dimension must be > 0.");
        }

        int resolvedMaxDegree = parallel
            ? maxDegree ?? Environment.ProcessorCount
            : 0;

        if (parallel && resolvedMaxDegree <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegree));
        }

        int resolvedSeed = seed ?? 0;
        int resolvedEmbeddingSeed = embeddingSeed ?? resolvedSeed;

        int resolvedSnapshotEvery = snapshotEvery ?? 0;

        string? resolvedSnapshotStreamPath = snapshotStreamPath;

        return new Options(
            Seed: resolvedSeed,
            Ticks: ticks,
            Agents: agents,
            Mode: mode,
            Brain: brain,
            Episodes: episodes,
            Generations: generations,
            Population: population,
            ResumePath: resumePath,
            Parallel: parallel,
            MaxDegree: resolvedMaxDegree,
            ComparePathA: comparePathA,
            ComparePathB: comparePathB,
            GenomePath: genomePath,
            ScenariosPath: scenariosPath,
            OutDir: outDir,
            Snapshots: snapshots,
            SnapshotEvery: resolvedSnapshotEvery,
            SnapshotDirectory: null,
            SnapshotStreamPath: resolvedSnapshotStreamPath,
            Metrics: metrics,
            MetricsOut: metricsOut,
            ReplayInputPath: replayInputPath,
            ReplayOutputPath: replayOutputPath,
            EmbeddingPath: embeddingsPath,
            EmbeddingDimension: embeddingDimension,
            EmbeddingSeed: resolvedEmbeddingSeed);
    }

    internal static Options NormalizeOptions(Options options)
    {
        string outDir = options.OutDir;
        if (string.IsNullOrWhiteSpace(outDir))
        {
            outDir = Path.Combine(
                "artifacts",
                "runs",
                $"run_{options.Mode.ToString().ToLowerInvariant()}_seed{options.Seed}_ticks{options.Ticks}");
        }

        string? metricsOut = options.MetricsOut;
        if (options.Metrics && string.IsNullOrWhiteSpace(metricsOut))
        {
            metricsOut = Path.Combine(outDir, "metrics", "metrics.csv");
        }

        string? snapshotStreamPath = options.SnapshotStreamPath;
        if (options.Snapshots && string.IsNullOrWhiteSpace(snapshotStreamPath))
        {
            snapshotStreamPath = Path.Combine(outDir, "snapshots", "snapshots.jsonl");
        }

        string? snapshotDirectory = options.SnapshotDirectory;
        if (string.IsNullOrWhiteSpace(snapshotDirectory))
        {
            snapshotDirectory = Path.Combine(outDir, "snapshots");
        }

        string embeddingPath = options.EmbeddingPath;
        if (string.IsNullOrWhiteSpace(embeddingPath))
        {
            embeddingPath = Path.Combine(outDir, "embeddings", "embedding_registry.json");
        }

        return options with
        {
            OutDir = outDir,
            MetricsOut = metricsOut,
            SnapshotStreamPath = snapshotStreamPath,
            SnapshotDirectory = snapshotDirectory,
            EmbeddingPath = embeddingPath
        };
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
            _ => throw new ArgumentException(
                $"Invalid mode '{value}'. Use 'sim', 'episode', 'train', 'compare', 'benchmark', 'replay', or 'replay-verify'.")
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

    private static bool ShouldPrepareEmbeddings(RunMode mode)
    {
        return mode == RunMode.Sim
            || mode == RunMode.Episode
            || mode == RunMode.Train
            || mode == RunMode.Benchmark;
    }

    private static EmbeddingRegistry EnsureEmbeddingRegistry(Options options)
    {
        string path = options.EmbeddingPath;
        if (File.Exists(path))
        {
            return EmbeddingRegistry.Load(path);
        }

        EmbeddingRegistry registry = new(options.EmbeddingDimension, options.EmbeddingSeed);
        registry.Save(path);
        return registry;
    }

    private static IBrain CreateBrain(string brain)
    {
        return brain switch
        {
            "none" => new NoneBrain(),
            "neat" => new HeuristicBrain(),
            "fake" => new FakeBrain(),
            _ => throw new ArgumentException($"Unknown brain '{brain}'. Use 'none', 'neat', or 'fake'.")
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

        int inputCount = BrainIO.InputCount(simConfig.AgentVisionRays, simConfig.EmbeddingDimension);
        int outputCount = BrainIO.OutputCount(simConfig.ActionPreferenceCount);

        GenomeBrainFactory factory = new();
        return factory.CreateBrain(genome, inputCount, outputCount);
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

    private static SimulationConfig CreateGenerationConfig(
        SimulationConfig baseConfig,
        CurriculumSchedule curriculum,
        int generation)
    {
        CurriculumPhase phase = curriculum.GetPhase(generation);
        return baseConfig with
        {
            WorldWidth = phase.WorldSize,
            WorldHeight = phase.WorldSize,
            ThirstRatePerSecond = phase.ThirstRatePerSecond,
            WaterProximityBias01 = phase.WaterProximityBias01,
            ObstacleDensity01 = phase.ObstacleDensity01,
            TicksPerEpisode = phase.TicksPerEpisode
        };
    }

    private static (int AliveAtEnd, ulong FinalChecksum) ComputeEpisodeMetrics(
        Genome genome,
        SimulationConfig simConfig,
        int seed)
    {
        int inputCount = BrainIO.InputCount(simConfig.AgentVisionRays, simConfig.EmbeddingDimension);
        int outputCount = BrainIO.OutputCount(simConfig.ActionPreferenceCount);
        GenomeBrainFactory factory = new();
        IBrain brain = factory.CreateBrain(genome, inputCount, outputCount);
        EpisodeRunner runner = new(simConfig);
        EpisodeResult result = runner.RunEpisode(brain, seed, simConfig.TicksPerEpisode, 1);
        int aliveAtEnd = result.TicksSurvived >= result.TicksRequested ? 1 : 0;
        return (aliveAtEnd, result.Checksum);
    }

    internal static void RunTraining(Options options, SimulationConfig simConfig)
    {
        options = NormalizeOptions(options);
        Directory.CreateDirectory("artifacts");
        string logPath = Path.Combine("artifacts", "train_log.csv");
        List<RunMetricsRow>? metricsRows = options.Metrics ? new List<RunMetricsRow>() : null;

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
            int inputCount = BrainIO.InputCount(simConfig.AgentVisionRays, simConfig.EmbeddingDimension);
            int outputCount = BrainIO.OutputCount(simConfig.ActionPreferenceCount);
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

            if (metricsRows is not null)
            {
                int metricsSeed = baseSeed + ((options.Population + 1) * 1000);
                SimulationConfig generationConfig = CreateGenerationConfig(simConfig, curriculum, gen);
                (int aliveAtEnd, ulong finalChecksum) = bestGenome is null
                    ? (0, 0UL)
                    : ComputeEpisodeMetrics(bestGenome, generationConfig, metricsSeed);

                metricsRows.Add(new RunMetricsRow
                {
                    Index = result.Generation,
                    BestFitness = (float)result.BestFitness,
                    AvgFitness = (float)result.MeanFitness,
                    P50Fitness = MetricsStatistics.ComputePercentile(result.Fitnesses, 0.50),
                    P90Fitness = MetricsStatistics.ComputePercentile(result.Fitnesses, 0.90),
                    AliveAtEnd = aliveAtEnd,
                    FinalChecksum = finalChecksum
                });
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
            WriteTrainingManifest(options, evoConfig, logPath, bestOverallPath, finalResult, curriculum);
        }

        if (metricsRows is not null)
        {
            string metricsOut = options.MetricsOut
                ?? throw new InvalidOperationException("Metrics output path was not resolved.");
            CsvMetricsWriter.Write(metricsOut, metricsRows);
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

    private static RunManifest CreateBaseManifest(Options options)
    {
        string outDir = options.OutDir;
        string? metricsPath = options.Metrics
            ? GetRelativePathForManifest(outDir, options.MetricsOut)
            : null;
        if (options.Metrics && metricsPath is null)
        {
            throw new InvalidOperationException("Metrics path was not resolved.");
        }

        string? snapshotStreamPath = options.Snapshots
            ? GetRelativePathForManifest(outDir, options.SnapshotStreamPath)
            : null;
        if (options.Snapshots && snapshotStreamPath is null)
        {
            throw new InvalidOperationException("Snapshot stream path was not resolved.");
        }

        return new RunManifest
        {
            Version = 1,
            Mode = options.Mode.ToString().ToLowerInvariant(),
            Seed = options.Seed,
            Ticks = options.Ticks,
            OutDir = ".",
            MetricsPath = metricsPath,
            ReplayPath = null,
            SnapshotStreamPath = snapshotStreamPath
        };
    }

    internal static RunManifest WriteOutputManifest(Options options)
    {
        options = NormalizeOptions(options);
        RunManifest manifest = CreateBaseManifest(options);
        WriteRunManifestFile(options, manifest);
        return manifest;
    }

    private static void WriteTrainingManifest(
        Options options,
        EvolutionConfig evoConfig,
        string logPath,
        string? bestGenomePath,
        GenerationResult finalResult,
        CurriculumSchedule curriculum)
    {
        CurriculumPhase startPhase = curriculum.GetPhase(0);
        CurriculumPhase endPhase = curriculum.GetPhase(curriculum.TotalGenerations - 1);
        RunManifest manifest = CreateBaseManifest(options) with
        {
            SchemaVersion = 2,
            CreatedUtc = string.Empty,
            Generations = options.Generations,
            Population = options.Population,
            EpisodesPerGenome = options.Episodes,
            TicksPerEpisode = options.Ticks,
            Parallel = options.Parallel ? 1 : 0,
            MaxDegree = options.MaxDegree,
            EvolutionConfig = new EvolutionConfigManifest(
                evoConfig.EliteCount,
                evoConfig.MutationRate,
                evoConfig.AddConnectionRate,
                evoConfig.AddNodeRate,
                evoConfig.WeightPerturbRate,
                evoConfig.WeightResetRate,
                evoConfig.MaxNodes,
                evoConfig.MaxConnections),
            CurriculumSchedule = new CurriculumScheduleManifest(
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
            BestGenomePath = GetRelativePathForManifest(options.OutDir, bestGenomePath) ?? string.Empty,
            CheckpointPath = GetRelativePathForManifest(options.OutDir, options.ResumePath) ?? string.Empty,
            TrainLogPath = GetRelativePathForManifest(options.OutDir, logPath) ?? string.Empty,
            FinalSummary = new RunManifestSummary(
                finalResult.BestFitness,
                finalResult.MeanFitness,
                finalResult.BestGenomeNodeCount,
                finalResult.BestGenomeConnectionCount,
                finalResult.BestTicksSurvived,
                finalResult.BestSuccessfulDrinks,
                finalResult.BestAvgThirst01)
        };

        WriteRunManifestFile(options, manifest);
    }

    private static void WriteRunManifestFile(Options options, RunManifest manifest)
    {
        Directory.CreateDirectory(options.OutDir);
        string manifestPath = Path.Combine(options.OutDir, "manifest.json");
        RunManifestJson.Write(manifestPath, manifest);
    }

    private static string? GetRelativePathForManifest(string outDir, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetRelativePath(outDir, path);
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

    private static void WriteSnapshotStream(
        string path,
        SnapshotStreamHeader header,
        IReadOnlyList<WorldSnapshot> snapshots)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (header is null)
        {
            throw new ArgumentNullException(nameof(header));
        }

        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        string? directory = Path.GetDirectoryName(path);

        if (directory is null)
        {
            throw new ArgumentException("Snapshot stream path must include a directory.", nameof(path));
        }

        Directory.CreateDirectory(directory);

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        JsonSerializerOptions options = SnapshotJson.GetWriteOptions();

        SnapshotStreamWriter.WriteHeader(writer, header, options);

        foreach (WorldSnapshot snapshot in snapshots)
        {
            SnapshotStreamWriter.WriteSnapshot(writer, snapshot, options);
        }
    }

    internal static ReplayRecord CreateReplayRecord(Options options, SimulationConfig config)
    {
        IBrain brain = CreateBrain(options.Brain);
        ReplayTickState[] tickStates = BuildReplayTicks(brain, config, options.Ticks, options.Agents, stopEarlyWhenAllAgentsDead: true);
        ulong checksum = tickStates.Length > 0 ? tickStates[^1].Checksum : 0UL;

        ReplayConfig replayConfig = new(
            config.Dt,
            config.WorldWidth,
            config.WorldHeight,
            config.AgentVisionRays,
            config.AgentVisionRange,
            config.AgentFov,
            config.EmbeddingDimension,
            config.EmbeddingSeed,
            config.AgentMaxSpeed,
            config.AgentTurnRateRad,
            config.MoveDeadzone,
            config.ThirstRatePerSecond,
            config.DeathGraceSeconds,
            config.WaterProximityBias01,
            config.ObstacleDensity01,
            config.ActionPreferenceCount);

        return new ReplayRecord(
            ReplayRecord.CurrentSchemaVersion,
            options.Seed,
            options.Ticks,
            options.Agents,
            options.Brain,
            replayConfig,
            checksum,
            tickStates);
    }

    internal static ReplayVerificationResult VerifyReplay(string path)
    {
        ReplayRecord replay = ReplayJson.Read(path);
        SimulationConfig config = BuildSimulationConfig(replay);
        IBrain brain = CreateBrain(replay.Brain);
        ReplayTickState[] expectedStates = replay.TickStates?.ToArray() ?? Array.Empty<ReplayTickState>();
        if (expectedStates.Length == 0)
        {
            EpisodeRunner runner = new(config);
            EpisodeResult result = runner.RunEpisode(brain, replay.Seed, replay.Ticks, replay.Agents);
            bool verified = result.Checksum == replay.Checksum;
            string? reason = verified ? null : "Checksum mismatch.";
            return new ReplayVerificationResult(verified, replay.Checksum, result.Checksum, null, reason);
        }

        ReplayTickState[] actualStates = BuildReplayTicks(brain, config, replay.Ticks, replay.Agents, stopEarlyWhenAllAgentsDead: true);
        int max = Math.Min(expectedStates.Length, actualStates.Length);
        for (int i = 0; i < max; i += 1)
        {
            ReplayTickState expected = expectedStates[i];
            ReplayTickState actual = actualStates[i];
            if (expected.Checksum != actual.Checksum)
            {
                string reason = DescribeMismatch(expected, actual);
                ulong actualChecksum = actualStates.Length > 0 ? actualStates[^1].Checksum : 0UL;
                return new ReplayVerificationResult(false, replay.Checksum, actualChecksum, expected.Tick, reason);
            }
        }

        if (expectedStates.Length != actualStates.Length)
        {
            int mismatchTick = max > 0 ? expectedStates[max - 1].Tick + 1 : 0;
            string reason = $"Tick count mismatch. Expected {expectedStates.Length}, actual {actualStates.Length}.";
            ulong actualChecksum = actualStates.Length > 0 ? actualStates[^1].Checksum : 0UL;
            return new ReplayVerificationResult(false, replay.Checksum, actualChecksum, mismatchTick, reason);
        }

        ulong finalChecksum = actualStates.Length > 0 ? actualStates[^1].Checksum : 0UL;
        bool finalVerified = finalChecksum == replay.Checksum;
        string? finalReason = finalVerified ? null : "Final checksum mismatch.";
        return new ReplayVerificationResult(finalVerified, replay.Checksum, finalChecksum, null, finalReason);
    }

    private static SimulationConfig BuildSimulationConfig(ReplayRecord replay)
    {
        ReplayConfig config = replay.Config;
        int embeddingDimension = config.EmbeddingDimension ?? DefaultEmbeddingDimension;
        int embeddingSeed = config.EmbeddingSeed ?? replay.Seed;
        return new SimulationConfig(
            replay.Seed,
            config.Dt,
            replay.Ticks,
            config.WorldWidth,
            config.WorldHeight,
            config.AgentVisionRays,
            config.AgentVisionRange,
            config.AgentFov,
            config.AgentMaxSpeed,
            config.AgentTurnRateRad,
            config.MoveDeadzone,
            config.ThirstRatePerSecond,
            config.DeathGraceSeconds,
            config.WaterProximityBias01,
            config.ObstacleDensity01,
            embeddingDimension,
            embeddingSeed,
            config.ActionPreferenceCount);
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
        string OutDir = "",
        bool Snapshots = false,
        int SnapshotEvery = 0,
        string? SnapshotDirectory = null,
        string? SnapshotStreamPath = null,
        bool Metrics = false,
        string? MetricsOut = null,
        string ReplayInputPath = "",
        string ReplayOutputPath = "",
        string EmbeddingPath = "",
        int EmbeddingDimension = 8,
        int EmbeddingSeed = 0);

    internal readonly record struct ManifestComparisonResult(bool Equivalent, IReadOnlyList<string> Differences);

    internal readonly record struct ReplayVerificationResult(
        bool Verified,
        ulong ExpectedChecksum,
        ulong ActualChecksum,
        int? MismatchTick,
        string? MismatchReason);

    private static ReplayTickState[] BuildReplayTicks(
        IBrain brain,
        SimulationConfig config,
        int ticks,
        int agents,
        bool stopEarlyWhenAllAgentsDead)
    {
        Simulation simulation = new(config, agents);
        List<ReplayTickState> states = new(ticks);

        for (int tick = 0; tick < ticks; tick += 1)
        {
            simulation.Step(brain);
            states.Add(BuildReplayTickState(simulation));

            if (stopEarlyWhenAllAgentsDead && AreAllAgentsDead(simulation.Agents))
            {
                break;
            }
        }

        return states.ToArray();
    }

    private static ReplayTickState BuildReplayTickState(Simulation simulation)
    {
        AgentState[] agents = simulation.Agents.ToArray();
        ReplayAgentState[] agentStates = new ReplayAgentState[agents.Length];
        for (int i = 0; i < agents.Length; i += 1)
        {
            AgentState agent = agents[i];
            agentStates[i] = new ReplayAgentState(
                agent.Position.X,
                agent.Position.Y,
                agent.HeadingRad,
                agent.Thirst01,
                agent.IsAlive);
        }

        ulong worldChecksum = simulation.World.ComputeChecksum();
        ulong agentsChecksum = SimulationChecksum.Compute(agents, simulation.Tick);
        ulong checksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);
        return new ReplayTickState(simulation.Tick, checksum, agentStates);
    }

    private static bool AreAllAgentsDead(IReadOnlyList<AgentState> agents)
    {
        foreach (AgentState agent in agents)
        {
            if (agent.IsAlive)
            {
                return false;
            }
        }

        return true;
    }

    private static string DescribeMismatch(ReplayTickState expected, ReplayTickState actual)
    {
        if (expected.Agents.Count != actual.Agents.Count)
        {
            return $"Agent count mismatch. Expected {expected.Agents.Count}, actual {actual.Agents.Count}.";
        }

        const float epsilon = 0.0001f;
        for (int i = 0; i < expected.Agents.Count; i += 1)
        {
            ReplayAgentState expectedAgent = expected.Agents[i];
            ReplayAgentState actualAgent = actual.Agents[i];

            if (MathF.Abs(expectedAgent.X - actualAgent.X) > epsilon || MathF.Abs(expectedAgent.Y - actualAgent.Y) > epsilon)
            {
                return $"Agent {i} position mismatch.";
            }

            if (MathF.Abs(expectedAgent.HeadingRad - actualAgent.HeadingRad) > epsilon)
            {
                return $"Agent {i} heading mismatch.";
            }

            if (MathF.Abs(expectedAgent.Thirst01 - actualAgent.Thirst01) > epsilon)
            {
                return $"Agent {i} thirst mismatch.";
            }

            if (expectedAgent.Alive != actualAgent.Alive)
            {
                return $"Agent {i} alive mismatch.";
            }
        }

        return "Checksum mismatch.";
    }

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
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput();
        }
    }

    private sealed class HeuristicBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput
            {
                ForwardSpeed = 0.5f,
                ActionPreferenceVector = new[] { input.Thirst01 > 0.6f ? 1f : -1f }
            };
        }
    }

    private sealed class FakeBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput
            {
                RotationDelta = -0.15f,
                ForwardSpeed = 0.25f,
                ActionPreferenceVector = new[] { input.Thirst01 > 0.5f ? 1f : -1f }
            };
        }
    }

}
