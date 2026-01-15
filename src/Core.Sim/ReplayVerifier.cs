namespace Core.Sim;

public readonly record struct ReplayVerificationResult(
    bool Success,
    ulong ExpectedChecksum,
    ulong ComputedChecksum,
    ulong ExpectedWorldChecksum,
    ulong ComputedWorldChecksum,
    int TicksSimulated);

public static class ReplayVerifier
{
    public static ReplayVerificationResult Verify(Replay replay, SimulationConfig baseConfig)
    {
        if (replay is null)
        {
            throw new ArgumentNullException(nameof(replay));
        }

        ReplayHeader header = replay.Header ?? throw new InvalidOperationException("Replay header is missing.");

        SimulationConfig config = baseConfig with
        {
            Seed = header.Seed,
            Dt = header.Dt,
            TicksPerEpisode = header.Ticks,
            WorldWidth = header.WorldWidth,
            WorldHeight = header.WorldHeight
        };

        Simulation simulation = new(config, agentCount: 1);
        ulong worldChecksum = simulation.World.ComputeChecksum();

        int ticksSimulated = 0;
        foreach (ReplayTick tick in replay.Ticks)
        {
            BrainOutput output = new(tick.OutMoveX, tick.OutMoveY, tick.OutDrink);

            simulation.Step(output);
            ticksSimulated += 1;
        }

        ulong agentsChecksum = SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
        ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);

        bool ticksMatch = replay.Header.Ticks == replay.Ticks.Count;
        bool worldMatch = worldChecksum == header.WorldChecksum;
        bool checksumMatch = totalChecksum == replay.FinalChecksum;
        bool success = ticksMatch && worldMatch && checksumMatch;

        return new ReplayVerificationResult(
            success,
            replay.FinalChecksum,
            totalChecksum,
            header.WorldChecksum,
            worldChecksum,
            ticksSimulated);
    }
}
