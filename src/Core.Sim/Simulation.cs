using System.Numerics;
using Core.Evo;

namespace Core.Sim;

public sealed class Simulation
{
    private const float DrinkAmount = 0.25f;
    private const float DrinkThreshold = 0.6f;
    private readonly List<Agent> _agents;
    private readonly SimRng _rng;
    private readonly VisionSensor _visionSensor;

    public Simulation(SimulationConfig config, int agentCount)
        : this(config, agentCount, _ => new NoOpBrain())
    {
    }

    public Simulation(SimulationConfig config, int agentCount, Func<int, IBrain> brainFactory)
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

        if (brainFactory is null)
        {
            throw new ArgumentNullException(nameof(brainFactory));
        }

        Config = config;
        _rng = new SimRng(config.Seed);
        _agents = new List<Agent>(agentCount);
        World = new GridWorld(config.WorldWidth, config.WorldHeight, config.Seed);
        _visionSensor = new VisionSensor(config.AgentVisionRays, config.AgentVisionRange, config.AgentFov);

        for (int i = 0; i < agentCount; i += 1)
        {
            Vector2 position = new Vector2(
                _rng.NextFloat(0f, config.WorldWidth),
                _rng.NextFloat(0f, config.WorldHeight));
            AgentState state = new(position);
            IBrain brain = brainFactory(i);
            _agents.Add(new Agent(state, brain));
        }
    }

    public int Tick { get; private set; }

    public SimulationConfig Config { get; }

    public GridWorld World { get; }

    public IReadOnlyList<Agent> Agents => _agents;

    public int DrinkActionsExecuted { get; private set; }

    public int SuccessfulDrinks { get; private set; }

    public void Step()
    {
        for (int i = 0; i < _agents.Count; i += 1)
        {
            Agent agent = _agents[i];
            AgentState state = agent.State;
            state.UpdateThirst(Config.Dt, Config.ThirstRatePerSecond, Config.DeathGraceSeconds);

            float[] vision = _visionSensor.Sense(World, state.Position, 0f);
            state.UpdateVision(vision);

            BrainInput input = new(vision, state.Thirst01);
            BrainOutput output = agent.Brain.Think(input);
            AgentAction action = DecideAction(output);
            ApplyAction(state, action);

            if (!state.IsAlive)
            {
                continue;
            }

            Vector2 delta = new Vector2(
                _rng.NextFloat(-1f, 1f),
                _rng.NextFloat(-1f, 1f));
            delta *= Config.Dt;
            state.TryApplyDelta(World, delta);
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

        if (action != AgentAction.Drink)
        {
            return;
        }

        DrinkActionsExecuted += 1;

        if (CanDrink(World, agent.Position))
        {
            SuccessfulDrinks += 1;
            agent.ApplyDrink(DrinkAmount);
        }
    }

    private static AgentAction DecideAction(BrainOutput output)
    {
        if (output.ActionDrinkScore > DrinkThreshold)
        {
            return AgentAction.Drink;
        }

        return AgentAction.None;
    }
}
