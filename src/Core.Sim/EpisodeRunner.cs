using System.Numerics;
using System.Collections.Generic;

namespace Core.Sim;

public sealed class EpisodeRunner
{
    private const int TargetVisitedCells = 50;
    private const int TargetDrinkableCells = 10;
    private readonly SimulationConfig _baseConfig;

    public EpisodeRunner(SimulationConfig baseConfig)
    {
        _baseConfig = baseConfig;
    }

    public Replay? LastReplay { get; private set; }

    public EpisodeResult RunEpisode(
        IBrain brain,
        int seed,
        int ticks,
        int agentCount = 1,
        bool captureReplay = false,
        Scenario? scenario = null,
        string? brainInfo = null)
    {
        if (brain is null)
        {
            throw new ArgumentNullException(nameof(brain));
        }

        if (ticks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        if (agentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(agentCount));
        }

        SimulationConfig config = _baseConfig with
        {
            Seed = seed,
            TicksPerEpisode = ticks
        };

        Simulation simulation = new(config, agentCount);
        return RunEpisodeInternal(brain, seed, ticks, simulation, captureReplay, scenario, brainInfo);
    }

    public EpisodeResult RunEpisode(IBrain brain, int seed, int ticks, IReadOnlyList<Vector2> initialAgentPositions)
    {
        if (brain is null)
        {
            throw new ArgumentNullException(nameof(brain));
        }

        if (ticks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        if (initialAgentPositions is null)
        {
            throw new ArgumentNullException(nameof(initialAgentPositions));
        }

        if (initialAgentPositions.Count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialAgentPositions));
        }

        SimulationConfig config = _baseConfig with
        {
            Seed = seed,
            TicksPerEpisode = ticks
        };

        Simulation simulation = new(config, initialAgentPositions);
        return RunEpisodeInternal(brain, seed, ticks, simulation, captureReplay: false, scenario: null, brainInfo: null);
    }

    private EpisodeResult RunEpisodeInternal(
        IBrain brain,
        int seed,
        int ticks,
        Simulation simulation,
        bool captureReplay,
        Scenario? scenario,
        string? brainInfo)
    {
        int ticksSurvived = 0;
        int successfulDrinks = 0;
        long thirstSumMilli = 0;
        long thirstSamples = 0;
        HashSet<(int X, int Y)> visitedCells = new();
        HashSet<(int X, int Y)> drinkableCellsVisited = new();
        List<ReplayTick>? replayTicks = null;

        if (captureReplay)
        {
            if (simulation.Agents.Count != 1)
            {
                throw new InvalidOperationException("Replay capture only supports a single agent.");
            }

            replayTicks = new List<ReplayTick>(ticks);
        }

        TrackVisitedCells(simulation, visitedCells, drinkableCellsVisited);

        for (int i = 0; i < ticks; i += 1)
        {
            BrainOutput output = default;
            int stepDrinks;

            if (captureReplay)
            {
                stepDrinks = simulation.Step(brain, out output);
            }
            else
            {
                stepDrinks = simulation.Step(brain);
            }

            successfulDrinks += stepDrinks;
            ticksSurvived += 1;

            TrackVisitedCells(simulation, visitedCells, drinkableCellsVisited);

            foreach (AgentState agent in simulation.Agents)
            {
                int thirstMilli = (int)MathF.Round(agent.Thirst01 * 1000f);
                thirstSumMilli += thirstMilli;
                thirstSamples += 1;
            }

            if (captureReplay && replayTicks is not null)
            {
                AgentState agent = simulation.Agents[0];
                replayTicks.Add(new ReplayTick
                {
                    Tick = i,
                    Ax = agent.Position.X,
                    Ay = agent.Position.Y,
                    Thirst01 = agent.Thirst01,
                    Alive = agent.IsAlive,
                    OutMoveX = output.MoveX,
                    OutMoveY = output.MoveY,
                    OutDrink = output.Drink01,
                    SuccessfulDrinksTotal = successfulDrinks,
                    VisitedCells = visitedCells.Count
                });
            }

            if (AreAllAgentsDead(simulation.Agents))
            {
                break;
            }
        }

        double avgThirst = thirstSamples > 0
            ? thirstSumMilli / (double)thirstSamples / 1000.0
            : 0.0;
        double quantizedAvgThirst = Math.Round(avgThirst, 6, MidpointRounding.AwayFromZero);
        float avgThirst01 = (float)quantizedAvgThirst;
        double distanceTraveled = Math.Round(simulation.DistanceTraveled, 6, MidpointRounding.AwayFromZero);
        int visitedCellCount = visitedCells.Count;
        int drinkableCellCount = drinkableCellsVisited.Count;

        double exploreScore = Math.Clamp(visitedCellCount / (double)TargetVisitedCells, 0.0, 1.0);
        double drinkableExploreScore = Math.Clamp(drinkableCellCount / (double)TargetDrinkableCells, 0.0, 1.0);

        double thirstScore = 1.0 - avgThirst01;
        double fitness = (ticksSurvived * 1000.0)
            + (thirstScore * 20.0)
            + (successfulDrinks * 5.0)
            + (exploreScore * 30.0)
            + (drinkableExploreScore * 20.0);

        ulong worldChecksum = simulation.World.ComputeChecksum();
        ulong agentsChecksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
        ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);

        if (captureReplay && replayTicks is not null)
        {
            LastReplay = new Replay
            {
                Header = new ReplayHeader
                {
                    Seed = seed,
                    Scenario = scenario,
                    WorldWidth = simulation.Config.WorldWidth,
                    WorldHeight = simulation.Config.WorldHeight,
                    Dt = simulation.Config.Dt,
                    Ticks = replayTicks.Count,
                    WorldChecksum = worldChecksum,
                    BrainInfo = brainInfo ?? string.Empty
                },
                Ticks = replayTicks,
                FinalChecksum = totalChecksum
            };
        }
        else
        {
            LastReplay = null;
        }

        return new EpisodeResult(
            seed,
            ticks,
            ticksSurvived,
            successfulDrinks,
            avgThirst01,
            distanceTraveled,
            visitedCellCount,
            drinkableCellCount,
            exploreScore,
            drinkableExploreScore,
            fitness,
            totalChecksum);
    }

    private static bool AreAllAgentsDead(IReadOnlyList<AgentState> agents)
    {
        foreach (AgentState agent in agents)
        {
            if (agent.IsAlive)
            {
                return false;
            }
        }

        return true;
    }

    private static void TrackVisitedCells(
        Simulation simulation,
        HashSet<(int X, int Y)> visitedCells,
        HashSet<(int X, int Y)> drinkableCellsVisited)
    {
        foreach (AgentState agent in simulation.Agents)
        {
            if (!agent.IsAlive)
            {
                continue;
            }

            (int X, int Y) cell = QuantizePosition(agent.Position);
            visitedCells.Add(cell);

            if (Simulation.CanDrink(simulation.World, agent.Position))
            {
                drinkableCellsVisited.Add(cell);
            }
        }
    }

    private static (int X, int Y) QuantizePosition(Vector2 position)
    {
        int x = (int)MathF.Floor(position.X);
        int y = (int)MathF.Floor(position.Y);
        return (x, y);
    }
}
