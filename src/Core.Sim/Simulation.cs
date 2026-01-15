using System.Numerics;
using Core.Evo;

namespace Core.Sim;

public sealed class Simulation
{
    private const float DrinkAmount = 0.25f;
    private const float DrinkThreshold = 0.6f;
    private const int SpawnWaterRadius = 3;
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
        Vector2 spawnHint = new(
            _rng.NextFloat(0f, config.WorldWidth),
            _rng.NextFloat(0f, config.WorldHeight));
        World = new GridWorld(
            config.WorldWidth,
            config.WorldHeight,
            config.Seed,
            config.ObstacleDensity01,
            config.WaterProximityBias01,
            spawnHint);
        _visionSensor = new VisionSensor(config.AgentVisionRays, config.AgentVisionRange, config.AgentFov);

        for (int i = 0; i < agentCount; i += 1)
        {
            bool useBias = i == 0 || _rng.NextFloat() < config.WaterProximityBias01;
            Vector2 position = useBias
                ? FindSpawnNearWater(World, spawnHint, SpawnWaterRadius)
                : new Vector2(
                    _rng.NextFloat(0f, config.WorldWidth),
                    _rng.NextFloat(0f, config.WorldHeight));
            _agents.Add(new AgentState(position, CreateVisionBuffer()));
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
        Vector2? spawnHint = initialPositions.Count > 0 ? initialPositions[0] : null;
        World = new GridWorld(
            config.WorldWidth,
            config.WorldHeight,
            config.Seed,
            config.ObstacleDensity01,
            config.WaterProximityBias01,
            spawnHint);
        _visionSensor = new VisionSensor(config.AgentVisionRays, config.AgentVisionRange, config.AgentFov);

        foreach (Vector2 position in initialPositions)
        {
            _agents.Add(new AgentState(position, CreateVisionBuffer()));
        }
    }

    public int Tick { get; private set; }

    public SimulationConfig Config { get; }

    public GridWorld World { get; }

    public IReadOnlyList<AgentState> Agents => _agents;

    public double DistanceTraveled { get; private set; }

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
            ApplyMovement(agent, delta);
            float[] vision = agent.LastVision;
            _visionSensor.Sense(World, agent.Position, 0f, vision);
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

            BrainInput input = new(agent.LastVision, agent.Thirst01, 1f);
            BrainOutput output = brain.DecideAction(input);
            Vector2 delta = ComputeMovementDelta(output);
            ApplyMovement(agent, delta);

            float thirstBeforeAction = agent.Thirst01;
            if (output.ActionDrinkScore > DrinkThreshold)
            {
                ApplyAction(agent, AgentAction.Drink);
            }

            if (agent.Thirst01 < thirstBeforeAction)
            {
                successfulDrinks += 1;
            }

            if (!agent.IsAlive)
            {
                continue;
            }

            float[] vision = agent.LastVision;
            _visionSensor.Sense(World, agent.Position, 0f, vision);
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

    private float[] CreateVisionBuffer()
    {
        return new float[_visionSensor.RayCount * 3];
    }

    private void ApplyMovement(AgentState agent, Vector2 delta)
    {
        if (delta == Vector2.Zero)
        {
            agent.UpdateDirection(Vector2.Zero);
            return;
        }

        float magnitude = delta.Length();
        Vector2 direction = magnitude > 0f
            ? delta / magnitude
            : Vector2.Zero;
        agent.UpdateDirection(direction);

        Vector2 start = agent.Position;
        if (agent.TryApplyDelta(World, delta))
        {
            DistanceTraveled += Vector2.Distance(start, agent.Position);
        }
    }

    private Vector2 ComputeMovementDelta(BrainOutput output)
    {
        Vector2 direction = new Vector2(output.MoveX, output.MoveY);
        float magnitude = direction.Length();

        if (magnitude < Config.MoveDeadzone)
        {
            return Vector2.Zero;
        }

        if (magnitude > 1f)
        {
            direction /= magnitude;
            magnitude = 1f;
        }

        float speed = Config.AgentMaxSpeed * magnitude;
        return direction * (speed * Config.Dt);
    }

    private static Vector2 FindSpawnNearWater(GridWorld world, Vector2 anchor, int radius)
    {
        int anchorX = (int)MathF.Floor(anchor.X);
        int anchorY = (int)MathF.Floor(anchor.Y);

        for (int r = 0; r <= radius; r += 1)
        {
            for (int dy = -r; dy <= r; dy += 1)
            {
                for (int dx = -r; dx <= r; dx += 1)
                {
                    int x = anchorX + dx;
                    int y = anchorY + dy;
                    if (!world.InBounds(x, y))
                    {
                        continue;
                    }

                    if (world.GetTile(x, y).Id != TileId.Empty)
                    {
                        continue;
                    }

                    if (HasWaterWithinRadius(world, x, y, radius))
                    {
                        return new Vector2(x + 0.5f, y + 0.5f);
                    }
                }
            }
        }

        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                if (world.GetTile(x, y).Id != TileId.Empty)
                {
                    continue;
                }

                if (HasWaterWithinRadius(world, x, y, radius))
                {
                    return new Vector2(x + 0.5f, y + 0.5f);
                }
            }
        }

        return new Vector2(
            Math.Clamp(anchor.X, 0f, Math.Max(0f, world.Width - 0.01f)),
            Math.Clamp(anchor.Y, 0f, Math.Max(0f, world.Height - 0.01f)));
    }

    private static bool HasWaterWithinRadius(GridWorld world, int tileX, int tileY, int radius)
    {
        for (int dy = -radius; dy <= radius; dy += 1)
        {
            for (int dx = -radius; dx <= radius; dx += 1)
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
}
