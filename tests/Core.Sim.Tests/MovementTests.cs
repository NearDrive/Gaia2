using System.Numerics;
using Core.Evo;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class MovementTests
{
    [Fact]
    public void Movement_ClampsForwardSpeed()
    {
        SimulationConfig config = CreateConfig(agentMaxSpeed: 0.4f, moveDeadzone: 0.05f, seed: 10);
        Vector2 start = FindEmptyTilePosition(config);
        Simulation simulation = new(config, new[] { start });
        IBrain brain = new ConstantBrain(new BrainOutput { ForwardSpeed = 2f });

        simulation.Step(brain);

        float distance = Vector2.Distance(start, simulation.Agents[0].Position);
        Assert.True(distance <= (config.AgentMaxSpeed * config.Dt) + 0.0001f);
    }

    [Fact]
    public void Agent_Moves_Forward_WithZeroRotation()
    {
        SimulationConfig config = CreateConfig(agentMaxSpeed: 0.2f, moveDeadzone: 0.05f, seed: 20);
        Vector2 start = FindEmptyTilePosition(config);
        Simulation simulation = new(config, new[] { start });
        IBrain brain = new ConstantBrain(new BrainOutput { ForwardSpeed = 1f, RotationDelta = 0f });

        simulation.Step(brain);

        Assert.True(simulation.Agents[0].Position.X > start.X);
    }

    [Fact]
    public void Agent_DoesNotMove_InDeadzone()
    {
        SimulationConfig config = CreateConfig(agentMaxSpeed: 0.4f, moveDeadzone: 0.05f, seed: 30);
        Vector2 start = FindEmptyTilePosition(config);
        Simulation simulation = new(config, new[] { start });
        IBrain brain = new ConstantBrain(new BrainOutput { ForwardSpeed = 0.01f });

        simulation.Step(brain);

        Assert.Equal(start, simulation.Agents[0].Position);
    }

    [Fact]
    public void Agent_Turns_WithPositiveRotation()
    {
        SimulationConfig config = CreateConfig(
            agentMaxSpeed: 0.3f,
            moveDeadzone: 0.05f,
            seed: 40,
            turnRateRad: MathF.PI / 2f);
        Vector2 start = FindEmptyTilePosition(config);
        Simulation simulation = new(config, new[] { start });
        IBrain brain = new ConstantBrain(new BrainOutput { RotationDelta = 1f, ForwardSpeed = 1f });

        simulation.Step(brain);

        Assert.True(simulation.Agents[0].Position.Y > start.Y);
        Assert.True(simulation.Agents[0].HeadingRad > 0f);
    }

    [Fact]
    public void Agent_BlockedByWorldBoundary()
    {
        SimulationConfig config = CreateConfig(agentMaxSpeed: 2f, moveDeadzone: 0.01f, seed: 50, turnRateRad: MathF.PI);
        Vector2 start = FindEmptyLeftEdgePosition(config);
        Simulation simulation = new(config, new[] { start });
        IBrain brain = new ConstantBrain(new BrainOutput { RotationDelta = -1f, ForwardSpeed = 1f });

        simulation.Step(brain);

        Assert.Equal(start, simulation.Agents[0].Position);
    }

    [Fact]
    public void Simulation_Deterministic_WithMovement()
    {
        SimulationConfig config = CreateConfig(agentMaxSpeed: 0.3f, moveDeadzone: 0.05f, seed: 60);
        Vector2 start = FindEmptyTilePosition(config);
        IBrain brain = new ConstantBrain(new BrainOutput { RotationDelta = 0.2f, ForwardSpeed = 0.6f });

        Simulation first = new(config, new[] { start });
        Simulation second = new(config, new[] { start });

        for (int i = 0; i < 10; i += 1)
        {
            first.Step(brain);
            second.Step(brain);
        }

        ulong worldChecksum = first.World.ComputeChecksum();
        ulong firstChecksum = SimulationChecksum.Combine(worldChecksum, SimulationChecksum.Compute(first.Agents, first.Tick));
        ulong secondChecksum = SimulationChecksum.Combine(worldChecksum, SimulationChecksum.Compute(second.Agents, second.Tick));

        Assert.Equal(firstChecksum, secondChecksum);
    }

    private static SimulationConfig CreateConfig(float agentMaxSpeed, float moveDeadzone, int seed, float turnRateRad = MathF.PI / 4f)
    {
        return new SimulationConfig(
            seed: seed,
            dt: 1f,
            ticksPerEpisode: 10,
            worldWidth: 16,
            worldHeight: 16,
            agentVisionRays: 4,
            agentVisionRange: 6f,
            agentFov: MathF.PI / 2f,
            agentMaxSpeed: agentMaxSpeed,
            agentTurnRateRad: turnRateRad,
            moveDeadzone: moveDeadzone);
    }

    private static Vector2 FindEmptyTilePosition(SimulationConfig config)
    {
        GridWorld world = new(config.WorldWidth, config.WorldHeight, config.Seed);
        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                if (!world.IsBlocked(x, y))
                {
                    return new Vector2(x + 0.5f, y + 0.5f);
                }
            }
        }

        throw new InvalidOperationException("No empty tile found in test world.");
    }

    private static Vector2 FindEmptyLeftEdgePosition(SimulationConfig config)
    {
        GridWorld world = new(config.WorldWidth, config.WorldHeight, config.Seed);
        for (int y = 0; y < world.Height; y += 1)
        {
            if (!world.IsBlocked(0, y))
            {
                return new Vector2(0.1f, y + 0.5f);
            }
        }

        throw new InvalidOperationException("No empty edge tile found in test world.");
    }

    private sealed class ConstantBrain : IBrain
    {
        private readonly BrainOutput _output;

        public ConstantBrain(BrainOutput output)
        {
            _output = output;
        }

        public BrainOutput DecideAction(BrainInput input)
        {
            return _output;
        }
    }
}
