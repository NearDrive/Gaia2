using System.Numerics;

namespace Core.Sim;

public sealed class EpisodeRunner
{
    private readonly SimulationConfig _baseConfig;

    public EpisodeRunner(SimulationConfig baseConfig)
    {
        _baseConfig = baseConfig;
    }

    public EpisodeResult RunEpisode(IBrain brain, int seed, int ticks, int agentCount = 1)
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
        return RunEpisodeInternal(brain, seed, ticks, simulation);
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
        return RunEpisodeInternal(brain, seed, ticks, simulation);
    }

    private static EpisodeResult RunEpisodeInternal(IBrain brain, int seed, int ticks, Simulation simulation)
    {
        int ticksSurvived = 0;
        int successfulDrinks = 0;
        long thirstSumMilli = 0;
        long thirstSamples = 0;

        for (int i = 0; i < ticks; i += 1)
        {
            successfulDrinks += simulation.Step(brain);
            ticksSurvived += 1;

            foreach (AgentState agent in simulation.Agents)
            {
                int thirstMilli = (int)MathF.Round(agent.Thirst01 * 1000f);
                thirstSumMilli += thirstMilli;
                thirstSamples += 1;
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

        double thirstScore = 1.0 - avgThirst01;
        double fitness = (ticksSurvived * 1000.0)
            + (thirstScore * 20.0)
            + (successfulDrinks * 5.0);

        ulong worldChecksum = simulation.World.ComputeChecksum();
        ulong agentsChecksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
        ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);

        return new EpisodeResult(
            seed,
            ticks,
            ticksSurvived,
            successfulDrinks,
            avgThirst01,
            distanceTraveled,
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
}
