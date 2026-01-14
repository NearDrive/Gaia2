using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class SimulationDeterminismTests
{
    [Fact]
    public void SameSeed_SameChecksum()
    {
        const int seed = 12345;
        const int ticks = 10000;
        const int agents = 32;
        const float dt = 1f;

        ulong first = RunSimulation(seed, ticks, agents, dt);
        ulong second = RunSimulation(seed, ticks, agents, dt);

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentSeed_DifferentChecksum()
    {
        const int ticks = 10000;
        const int agents = 32;
        const float dt = 1f;

        ulong first = RunSimulation(111, ticks, agents, dt);
        ulong second = RunSimulation(222, ticks, agents, dt);

        Assert.NotEqual(first, second);
    }

    private static ulong RunSimulation(int seed, int ticks, int agents, float dt)
    {
        SimulationConfig config = new(seed, dt, ticks);
        Simulation simulation = new(config, agents);
        simulation.Run(ticks);

        return SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
    }
}
