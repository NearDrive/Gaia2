using System.Numerics;

namespace Core.Sim;

public sealed class Simulation
{
    private const float DrinkAmount = 0.25f;
    private const float DrinkThreshold = 0.6f;
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

    public Simulation(SimulationConfig config, IReadOnlyList<Vector2> initialPositions)
    {
        if (initialPositions is null)
        {
            throw new ArgumentNullException(nameof(initialPositions));
        }

        if (initialPositions.Count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialPositions));
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
        _agents = new List<AgentState>(initialPositions.Count);
        World = new GridWorld(config.WorldWidth, config.WorldHeight, config.Seed);
        _visionSensor = new VisionSensor(config.AgentVisionRays, config.AgentVisionRange, config.AgentFov);

        foreach (Vector2 position in initialPositions)
        {
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
            AgentState agent = _agents[i];
            agent.UpdateThirst(Config.Dt, Config.ThirstRatePerSecond, Config.DeathGraceSeconds);

            AgentAction action = DecideAction(agent);
            ApplyAction(agent, action);

            if (!agent.IsAlive)
            {
                continue;
            }

            Vector2 delta = new Vector2(
                _rng.NextFloat(-1f, 1f),
                _rng.NextFloat(-1f, 1f));
            delta *= Config.Dt;
            agent.TryApplyDelta(World, delta);
            float[] vision = _visionSensor.Sense(World, agent.Position, 0f);
            agent.UpdateVision(vision);
        }

        Tick += 1;
    }

    public int Step(IBrain brain)
    {
        if (brain is null)
        {
            throw new ArgumentNullException(nameof(brain));
        }

        int successfulDrinks = 0;

        for (int i = 0; i < _agents.Count; i += 1)
        {
            AgentState agent = _agents[i];
            agent.UpdateThirst(Config.Dt, Config.ThirstRatePerSecond, Config.DeathGraceSeconds);

            if (!agent.IsAlive)
            {
                continue;
            }

            float thirstBeforeAction = agent.Thirst01;
            AgentAction action = brain.DecideAction(agent, this);
            ApplyAction(agent, action);

            if (agent.Thirst01 < thirstBeforeAction)
            {
                successfulDrinks += 1;
            }

            if (!agent.IsAlive)
            {
                continue;
            }

            Vector2 delta = new Vector2(
                _rng.NextFloat(-1f, 1f),
                _rng.NextFloat(-1f, 1f));
            delta *= Config.Dt;
            agent.TryApplyDelta(World, delta);
            float[] vision = _visionSensor.Sense(World, agent.Position, 0f);
            agent.UpdateVision(vision);
        }

        Tick += 1;
        return successfulDrinks;
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

    public static bool CanDrink(GridWorld world, Vector2 position)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        int tileX = (int)MathF.Floor(position.X);
        int tileY = (int)MathF.Floor(position.Y);

        for (int dy = -1; dy <= 1; dy += 1)
        {
            for (int dx = -1; dx <= 1; dx += 1)
            {
                int nx = tileX + dx;
                int ny = tileY + dy;

                if (!world.InBounds(nx, ny))
                {
                    continue;
                }

                if (world.GetTile(nx, ny).Id == TileId.Water)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void ApplyAction(AgentState agent, AgentAction action)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (!agent.IsAlive)
        {
            return;
        }

        if (action == AgentAction.Drink && CanDrink(World, agent.Position))
        {
            agent.ApplyDrink(DrinkAmount);
        }
    }

    private AgentAction DecideAction(AgentState agent)
    {
        if (!agent.IsAlive)
        {
            return AgentAction.None;
        }

        if (agent.Thirst01 > DrinkThreshold && CanDrink(World, agent.Position))
        {
            return AgentAction.Drink;
        }

        return AgentAction.None;
    }
}
