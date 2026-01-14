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

        double survivalScore = ticksSurvived / (double)ticks;
        double thirstScore = 1.0 - avgThirst01;
        double drinkScore = Math.Clamp(successfulDrinks / 10.0, 0.0, 1.0);
        double fitness = (100.0 * survivalScore) + (20.0 * thirstScore) + (5.0 * drinkScore);

        ulong worldChecksum = simulation.World.ComputeChecksum();
        ulong agentsChecksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
        ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);

        return new EpisodeResult(
            seed,
            ticks,
            ticksSurvived,
            successfulDrinks,
            avgThirst01,
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
