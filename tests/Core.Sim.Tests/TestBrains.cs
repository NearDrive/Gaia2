using Core.Evo;
using Core.Sim;

namespace Core.Sim.Tests;

internal sealed class NoOpBrain : IBrain
{
    public BrainOutput DecideAction(BrainInput input)
    {
        return new BrainOutput();
    }
}
