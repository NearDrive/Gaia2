using Core.Sim;

const int DefaultTicks = 10000;
const int DefaultAgents = 32;
const float DefaultDt = 1f;
const int DefaultWorldWidth = 32;
const int DefaultWorldHeight = 32;
const int DefaultVisionRays = 8;
const float DefaultVisionRange = 10f;
const float DefaultVisionFov = MathF.PI / 2f;

(int seed, int ticks, int agents) = ParseArgs(args);

SimulationConfig config = new(
    seed,
    DefaultDt,
    ticks,
    DefaultWorldWidth,
    DefaultWorldHeight,
    DefaultVisionRays,
    DefaultVisionRange,
    DefaultVisionFov);
Simulation simulation = new(config, agents);
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

static (int Seed, int Ticks, int Agents) ParseArgs(string[] args)
{
    int? seed = null;
    int ticks = DefaultTicks;
    int agents = DefaultAgents;

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

    return (seed.Value, ticks, agents);
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
