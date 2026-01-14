namespace Core.Evo;

public sealed class NoOpBrain : IBrain
{
    public BrainOutput Think(BrainInput input)
    {
        return new BrainOutput(0f);
    }
}
