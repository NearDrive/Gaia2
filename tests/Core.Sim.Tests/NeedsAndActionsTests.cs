using System.Numerics;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class NeedsAndActionsTests
{
    [Fact]
    public void Thirst_Increases_OverTime()
    {
        AgentState agent = new(new Vector2(1.5f, 1.5f));

        agent.UpdateThirst(1f, 0.2f, 5f);

        Assert.True(agent.Thirst01 > 0f);
    }

    [Fact]
    public void Drink_ReducesThirst_WhenNearWater()
    {
        Simulation simulation = CreateSimulation(222);
        AgentState agent = simulation.Agents[0];
        Vector2 target = FindPositionAdjacentToWater(simulation.World);
        MoveAgentTo(simulation.World, agent, target);
        agent.SetThirst01(0.9f);

        float before = agent.Thirst01;
        simulation.ApplyAction(agent, AgentAction.Drink);

        Assert.True(agent.Thirst01 < before);
    }

    [Fact]
    public void Drink_DoesNothing_WhenNotNearWater()
    {
        Simulation simulation = CreateSimulation(333);
        AgentState agent = simulation.Agents[0];
        Vector2 target = FindPositionAwayFromWater(simulation.World);
        MoveAgentTo(simulation.World, agent, target);
        agent.SetThirst01(0.9f);

        float before = agent.Thirst01;
        simulation.ApplyAction(agent, AgentAction.Drink);

        Assert.Equal(before, agent.Thirst01);
    }

    [Fact]
    public void Agent_Dies_WhenThirstMaxed_LongEnough()
    {
        AgentState agent = new(new Vector2(1.5f, 1.5f));
        agent.SetThirst01(1f);

        agent.UpdateThirst(1f, 0f, 2f);
        agent.UpdateThirst(1f, 0f, 2f);

        Assert.False(agent.IsAlive);
    }

    private static Simulation CreateSimulation(int seed)
    {
        SimulationConfig config = new(
            seed,
            1f,
            100,
            32,
            32,
            8,
            10f,
            MathF.PI / 2f,
            1.5f,
            0.05f);
        return new Simulation(config, 1);
    }

    private static void MoveAgentTo(GridWorld world, AgentState agent, Vector2 target)
    {
        Vector2 delta = target - agent.Position;
        bool moved = agent.TryApplyDelta(world, delta);
        if (!moved)
        {
            throw new InvalidOperationException("Failed to move agent to target position.");
        }
    }

    private static Vector2 FindPositionAdjacentToWater(GridWorld world)
    {
        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                if (world.GetTile(x, y).Id != TileId.Water)
                {
                    continue;
                }

                for (int dy = -1; dy <= 1; dy += 1)
                {
                    for (int dx = -1; dx <= 1; dx += 1)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (!world.InBounds(nx, ny))
                        {
                            continue;
                        }

                        if (!world.IsBlocked(nx, ny))
                        {
                            return new Vector2(nx + 0.5f, ny + 0.5f);
                        }
                    }
                }
            }
        }

        throw new InvalidOperationException("No empty tile adjacent to water found.");
    }

    private static Vector2 FindPositionAwayFromWater(GridWorld world)
    {
        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                if (world.IsBlocked(x, y))
                {
                    continue;
                }

                bool nearWater = false;
                for (int dy = -1; dy <= 1; dy += 1)
                {
                    for (int dx = -1; dx <= 1; dx += 1)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (!world.InBounds(nx, ny))
                        {
                            continue;
                        }

                        if (world.GetTile(nx, ny).Id == TileId.Water)
                        {
                            nearWater = true;
                            break;
                        }
                    }

                    if (nearWater)
                    {
                        break;
                    }
                }

                if (!nearWater)
                {
                    return new Vector2(x + 0.5f, y + 0.5f);
                }
            }
        }

        throw new InvalidOperationException("No empty tile away from water found.");
    }
}
