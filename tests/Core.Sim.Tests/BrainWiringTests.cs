using System.Numerics;
using Core.Evo;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class BrainWiringTests
{
    [Fact]
    public void Brain_Threshold_DecidesDrink()
    {
        SimulationConfig config = new(
            100,
            1f,
            1,
            8,
            8,
            4,
            6f,
            MathF.PI / 2f);

        Simulation drinkSim = new(config, 1, _ => new FakeBrain(1f));
        drinkSim.Step();

        Simulation noDrinkSim = new(config, 1, _ => new FakeBrain(0f));
        noDrinkSim.Step();

        Assert.Equal(1, drinkSim.DrinkActionsExecuted);
        Assert.Equal(0, noDrinkSim.DrinkActionsExecuted);
    }

    [Fact]
    public void Simulation_WithBrain_IsDeterministic()
    {
        const int seed = 4242;
        const int ticks = 200;
        const int agents = 4;
        SimulationConfig config = new(
            seed,
            1f,
            ticks,
            16,
            16,
            6,
            10f,
            MathF.PI / 2f);

        ulong first = Run(config, agents, ticks);
        ulong second = Run(config, agents, ticks);

        Assert.Equal(first, second);
    }

    private static ulong Run(SimulationConfig config, int agents, int ticks)
    {
        Simulation simulation = new(config, agents, _ => new FakeBrain(0.75f));
        simulation.Run(ticks);
        return SimulationChecksum.Compute(simulation.Agents, simulation.Tick);
    }

    private sealed class FakeBrain : IBrain
    {
        private readonly float _score;

        public FakeBrain(float score)
        {
            _score = score;
        }

        public BrainOutput Think(BrainInput input)
        {
            return new BrainOutput(_score);
        }
    }
}
