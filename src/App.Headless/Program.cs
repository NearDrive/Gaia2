using Core.Evo;
using Core.Sim;

const int DefaultTicks = 10000;
const int DefaultAgents = 32;
const float DefaultDt = 1f;
const int DefaultWorldWidth = 32;
const int DefaultWorldHeight = 32;
const int DefaultVisionRays = 8;
const float DefaultVisionRange = 10f;
const float DefaultVisionFov = MathF.PI / 2f;

(int seed, int ticks, int agents, BrainMode brainMode, string? genomePath) = ParseArgs(args);

SimulationConfig config = new(
    seed,
    DefaultDt,
    ticks,
    DefaultWorldWidth,
    DefaultWorldHeight,
    DefaultVisionRays,
    DefaultVisionRange,
    DefaultVisionFov);
Func<int, IBrain> brainFactory = CreateBrainFactory(brainMode, genomePath, config);
Simulation simulation = new(config, agents, brainFactory);
simulation.Run(ticks);

ulong worldChecksum = simulation.World.ComputeChecksum();
ulong agentsChecksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);
(int aliveCount, float minThirst, float avgThirst, float maxThirst) = SummarizeThirst(simulation.Agents);

Console.WriteLine($"Seed: {seed}");
Console.WriteLine($"Ticks: {ticks}");
Console.WriteLine($"Agents: {agents}");
Console.WriteLine($"Alive: {aliveCount}");
Console.WriteLine($"Thirst01 Min/Avg/Max: {minThirst:0.000}/{avgThirst:0.000}/{maxThirst:0.000}");
Console.WriteLine($"WorldChecksum: {worldChecksum}");
Console.WriteLine($"AgentsChecksum: {agentsChecksum}");
Console.WriteLine($"TotalChecksum: {totalChecksum}");
Console.WriteLine($"DrinkActionsExecuted: {simulation.DrinkActionsExecuted}");
Console.WriteLine($"SuccessfulDrinks: {simulation.SuccessfulDrinks}");

static (int Seed, int Ticks, int Agents, BrainMode BrainMode, string? GenomePath) ParseArgs(string[] args)
{
    int? seed = null;
    int ticks = DefaultTicks;
    int agents = DefaultAgents;
    BrainMode brainMode = BrainMode.None;
    string? genomePath = null;

    for (int i = 0; i < args.Length; i += 1)
    {
        string arg = args[i];
        switch (arg)
        {
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
                brainMode = ParseBrainMode(args, ref i);
                break;
            case "--genome":
                genomePath = ParseStringValue(args, ref i, "--genome");
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

    return (seed.Value, ticks, agents, brainMode, genomePath);
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

static BrainMode ParseBrainMode(string[] args, ref int index)
{
    string value = ParseStringValue(args, ref index, "--brain");
    return value switch
    {
        "none" => BrainMode.None,
        "neat" => BrainMode.Neat,
        _ => throw new ArgumentException($"Invalid value for --brain: '{value}'.")
    };
}

static (int AliveCount, float MinThirst, float AvgThirst, float MaxThirst) SummarizeThirst(
    IReadOnlyList<Agent> agents)
{
    if (agents.Count == 0)
    {
        return (0, 0f, 0f, 0f);
    }

    int alive = 0;
    float min = float.MaxValue;
    float max = float.MinValue;
    float total = 0f;

    foreach (Agent agent in agents)
    {
        AgentState state = agent.State;
        if (state.IsAlive)
        {
            alive += 1;
        }

        float thirst = Quantize(state.Thirst01);
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

static Func<int, IBrain> CreateBrainFactory(BrainMode brainMode, string? genomePath, SimulationConfig config)
{
    return brainMode switch
    {
        BrainMode.None => _ => new NoOpBrain(),
        BrainMode.Neat => CreateNeatFactory(genomePath, config),
        _ => throw new ArgumentOutOfRangeException(nameof(brainMode))
    };
}

static Func<int, IBrain> CreateNeatFactory(string? genomePath, SimulationConfig config)
{
    int visionInputs = config.AgentVisionRays * 3;
    int inputCount = visionInputs + 1;
    NeatGenome genome = LoadOrCreateGenome(genomePath, inputCount);
    return _ => new NeatBrain(genome);
}

static NeatGenome LoadOrCreateGenome(string? genomePath, int inputCount)
{
    if (string.IsNullOrWhiteSpace(genomePath))
    {
        return NeatGenome.CreateDeterministic(inputCount);
    }

    if (File.Exists(genomePath))
    {
        return NeatGenome.Load(genomePath);
    }

    NeatGenome genome = NeatGenome.CreateDeterministic(inputCount);
    Directory.CreateDirectory(Path.GetDirectoryName(genomePath) ?? ".");
    genome.Save(genomePath);
    return genome;
}

enum BrainMode
{
    None,
    Neat
}
