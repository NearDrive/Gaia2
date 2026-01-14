using System.Numerics;

namespace Core.Sim;

public sealed class Simulation
{
    private readonly List<AgentState> _agents;
    private readonly SimRng _rng;

    public Simulation(SimulationConfig config, int agentCount)
    {
        if (agentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(agentCount));
        }

        Config = config;
        _rng = new SimRng(config.Seed);
        _agents = new List<AgentState>(agentCount);

        for (int i = 0; i < agentCount; i += 1)
        {
            Vector2 position = new Vector2(
                _rng.NextFloat(-1f, 1f),
                _rng.NextFloat(-1f, 1f));
            _agents.Add(new AgentState(position));
        }
    }

    public int Tick { get; private set; }

    public SimulationConfig Config { get; }

    public IReadOnlyList<AgentState> Agents => _agents;

    public void Step()
    {
        for (int i = 0; i < _agents.Count; i += 1)
        {
            Vector2 delta = new Vector2(
                _rng.NextFloat(-1f, 1f),
                _rng.NextFloat(-1f, 1f));
            delta *= Config.Dt;
            _agents[i].ApplyDelta(delta);
        }

        Tick += 1;
    }

    public void Run(int ticks)
    {
        if (ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        for (int i = 0; i < ticks; i += 1)
        {
            Step();
        }
    }
}
