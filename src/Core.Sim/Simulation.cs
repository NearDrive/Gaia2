using System.Numerics;

namespace Core.Sim;

public sealed class Simulation
{
    private readonly List<AgentState> _agents;
    private readonly SimRng _rng;
    private readonly VisionSensor _visionSensor;

    public Simulation(SimulationConfig config, int agentCount)
    {
        if (agentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(agentCount));
        }

        if (config.WorldWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config.WorldWidth));
        }

        if (config.WorldHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config.WorldHeight));
        }

        Config = config;
        _rng = new SimRng(config.Seed);
        _agents = new List<AgentState>(agentCount);
        World = new GridWorld(config.WorldWidth, config.WorldHeight, config.Seed);
        _visionSensor = new VisionSensor(config.AgentVisionRays, config.AgentVisionRange, config.AgentFov);

        for (int i = 0; i < agentCount; i += 1)
        {
            Vector2 position = new Vector2(
                _rng.NextFloat(0f, config.WorldWidth),
                _rng.NextFloat(0f, config.WorldHeight));
            _agents.Add(new AgentState(position));
        }
    }

    public int Tick { get; private set; }

    public SimulationConfig Config { get; }

    public GridWorld World { get; }

    public IReadOnlyList<AgentState> Agents => _agents;

    public void Step()
    {
        for (int i = 0; i < _agents.Count; i += 1)
        {
            Vector2 delta = new Vector2(
                _rng.NextFloat(-1f, 1f),
                _rng.NextFloat(-1f, 1f));
            delta *= Config.Dt;
            _agents[i].TryApplyDelta(World, delta);
            float[] vision = _visionSensor.Sense(World, _agents[i].Position, 0f);
            _agents[i].UpdateVision(vision);
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
