using Core.Evo;
using Xunit;

namespace Core.Evo.Tests;

public class NeatBrainTests
{
    [Fact]
    public void NeatBrain_IsDeterministic_ForSameInput()
    {
        int inputCount = 5;
        NeatGenome genome = NeatGenome.CreateDeterministic(inputCount);
        NeatBrain brain = new(genome);
        float[] vision = { 0.1f, 0.2f, 0.3f, 0.4f };
        BrainInput input = new(vision, 0.75f);

        BrainOutput first = brain.Think(input);
        BrainOutput second = brain.Think(input);

        Assert.Equal(first.ActionDrinkScore, second.ActionDrinkScore);
    }
}
