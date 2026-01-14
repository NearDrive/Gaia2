using Core.Sim;

const int DefaultTicks = 10000;
const int DefaultAgents = 32;
const float DefaultDt = 1f;

(int seed, int ticks, int agents) = ParseArgs(args);

SimulationConfig config = new(seed, DefaultDt, ticks);
Simulation simulation = new(config, agents);
simulation.Run(ticks);

ulong checksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);

Console.WriteLine($"Seed: {seed}");
Console.WriteLine($"Ticks: {ticks}");
Console.WriteLine($"Agents: {agents}");
Console.WriteLine($"Checksum: {checksum}");

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
