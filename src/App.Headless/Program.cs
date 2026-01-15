using System.Globalization;
using System.Text.Json;
using Core.Evo;
using Core.Sim;

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

    public static int Main(string[] args)
    {
        Options options = ParseArgs(args);

        SimulationConfig config = new(
            options.Seed,
            DefaultDt,
            options.Ticks,
            DefaultWorldWidth,
            DefaultWorldHeight,
            DefaultVisionRays,
            DefaultVisionRange,
            DefaultVisionFov);

        if (options.Mode == RunMode.Sim)
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

            Console.WriteLine("Seed,TicksSurvived,SuccessfulDrinks,AvgThirst01,Fitness,Checksum");

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
        RunMode mode = RunMode.Sim;
        string brain = "none";

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
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }
        }

        if (!seed.HasValue && mode != RunMode.Train)
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

        return new Options(seed!.Value, ticks, agents, mode, brain, episodes, generations, population);
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
            _ => throw new ArgumentException($"Invalid mode '{value}'. Use 'sim', 'episode', or 'train'.")
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

    private static void RunTraining(Options options, SimulationConfig simConfig)
    {
        EvolutionConfig evoConfig = new(
            options.Population,
            eliteCount: 4,
            mutationRate: 0.8,
            addConnectionRate: 0.1,
            addNodeRate: 0.03,
            weightPerturbRate: 0.8,
            weightResetRate: 0.1,
            maxNodes: 64,
            maxConnections: 256);

        Evolver evolver = new(evoConfig);
        int inputCount = (simConfig.AgentVisionRays * 3) + 1;
        int outputCount = 1;
        Population population = evolver.CreateInitialPopulation(
            options.Seed,
            options.Population,
            inputCount,
            outputCount);

        Trainer trainer = new();
        Directory.CreateDirectory("artifacts");
        Genome? bestOverall = null;
        double bestOverallFitness = double.MinValue;

        for (int gen = 0; gen < options.Generations; gen += 1)
        {
            int baseSeed = options.Seed + (gen * 10000);
            GenerationResult result = trainer.RunGeneration(
                population,
                baseSeed,
                options.Episodes,
                simConfig,
                options.Ticks);

            Genome? bestGenome = result.BestGenome;
            if (bestGenome is not null && result.BestFitness > bestOverallFitness)
            {
                bestOverallFitness = result.BestFitness;
                bestOverall = bestGenome;
            }

            Console.WriteLine(
                $"Gen={result.Generation} Best={result.BestFitness:0.000} Mean={result.MeanFitness:0.000} " +
                $"Worst={result.WorstFitness:0.000} Nodes={bestGenome?.NodeCount ?? 0} Conns={bestGenome?.ConnectionCount ?? 0}");

            if (bestGenome is not null)
            {
                string generationPath = Path.Combine("artifacts", $"best_gen_gen{result.Generation}.json");
                WriteGenomeJson(bestGenome, generationPath);
            }

            population = evolver.NextGeneration(population, result.Fitnesses, options.Seed + gen + 1);
        }

        if (bestOverall is not null)
        {
            string bestPath = Path.Combine("artifacts", "best.json");
            WriteGenomeJson(bestOverall, bestPath);
        }
    }

    private static void WriteGenomeJson(Genome genome, string path)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        GenomeDto dto = new(
            genome.Nodes.Select(node => new NodeDto(node.Id, node.Type)).ToList(),
            genome.Connections.Select(connection => new ConnectionDto(
                connection.InNodeId,
                connection.OutNodeId,
                connection.Weight,
                connection.Enabled,
                connection.InnovationId)).ToList());

        string json = JsonSerializer.Serialize(dto, options);
        File.WriteAllText(path, json);
    }

    private enum RunMode
    {
        Sim,
        Episode,
        Train
    }

    private readonly record struct Options(
        int Seed,
        int Ticks,
        int Agents,
        RunMode Mode,
        string Brain,
        int Episodes,
        int Generations,
        int Population);

    private sealed class NoneBrain : IBrain
    {
        public AgentAction DecideAction(AgentState agent, Simulation simulation)
        {
            return AgentAction.None;
        }
    }

    private sealed class HeuristicBrain : IBrain
    {
        public AgentAction DecideAction(AgentState agent, Simulation simulation)
        {
            if (!agent.IsAlive)
            {
                return AgentAction.None;
            }

            if (agent.Thirst01 > 0.6f && Simulation.CanDrink(simulation.World, agent.Position))
            {
                return AgentAction.Drink;
            }

            return AgentAction.None;
        }
    }

    private sealed record GenomeDto(IReadOnlyList<NodeDto> Nodes, IReadOnlyList<ConnectionDto> Connections);

    private sealed record NodeDto(int Id, NodeType Type);

    private sealed record ConnectionDto(int InNodeId, int OutNodeId, double Weight, bool Enabled, int InnovationId);
}
