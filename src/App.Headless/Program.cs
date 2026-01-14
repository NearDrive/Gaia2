using System.Globalization;
using Core.Sim;

const int DefaultTicks = 10000;
const int DefaultAgents = 32;
const float DefaultDt = 1f;
const int DefaultWorldWidth = 32;
const int DefaultWorldHeight = 32;
const int DefaultVisionRays = 8;
const float DefaultVisionRange = 10f;
const float DefaultVisionFov = MathF.PI / 2f;

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
else
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

static Options ParseArgs(string[] args)
{
    int? seed = null;
    int ticks = DefaultTicks;
    int agents = DefaultAgents;
    int episodes = 1;
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
                break;
            case "--agents":
                agents = ParseIntValue(args, ref i, "--agents");
                break;
            case "--brain":
                brain = ParseStringValue(args, ref i, "--brain");
                break;
            case "--episodes":
                episodes = ParseIntValue(args, ref i, "--episodes");
                break;
            default:
                throw new ArgumentException($"Unknown argument '{arg}'.");
        }
    }

    if (!seed.HasValue)
    {
        throw new ArgumentException("Missing required --seed <int> argument.");
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

    if (mode == RunMode.Episode && episodes <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(episodes));
    }

    if (mode == RunMode.Episode && brain.Length == 0)
    {
        throw new ArgumentException("Missing required --brain <none|neat> argument for episode mode.");
    }

    return new Options(seed.Value, ticks, agents, mode, brain, episodes);
}

static int ParseIntValue(string[] args, ref int index, string name)
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

static string ParseStringValue(string[] args, ref int index, string name)
{
    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"Missing value for {name}.");
    }

    index += 1;
    return args[index];
}

static RunMode ParseModeValue(string[] args, ref int index)
{
    string value = ParseStringValue(args, ref index, "--mode");
    return value switch
    {
        "sim" => RunMode.Sim,
        "episode" => RunMode.Episode,
        _ => throw new ArgumentException($"Invalid mode '{value}'. Use 'sim' or 'episode'.")
    };
}

static (int AliveCount, float MinThirst, float AvgThirst, float MaxThirst) SummarizeThirst(
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

static float Quantize(float value)
{
    return MathF.Round(value * 1000f) / 1000f;
}

static IBrain CreateBrain(string brain)
{
    return brain switch
    {
        "none" => new NoneBrain(),
        "neat" => new HeuristicBrain(),
        _ => throw new ArgumentException($"Unknown brain '{brain}'. Use 'none' or 'neat'.")
    };
}

enum RunMode
{
    Sim,
    Episode
}

readonly record struct Options(
    int Seed,
    int Ticks,
    int Agents,
    RunMode Mode,
    string Brain,
    int Episodes);

sealed class NoneBrain : IBrain
{
    public AgentAction DecideAction(AgentState agent, Simulation simulation)
    {
        return AgentAction.None;
    }
}

sealed class HeuristicBrain : IBrain
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
