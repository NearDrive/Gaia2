namespace Core.Evo;

public sealed class NeatBrain : IBrain
{
    private readonly NeatGenome _genome;

    public NeatBrain(NeatGenome genome)
    {
        _genome = genome ?? throw new ArgumentNullException(nameof(genome));
        if (_genome.InputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(genome), "Genome must define a positive input count.");
        }

        if (_genome.Weights.Length != _genome.InputCount)
        {
            throw new ArgumentException("Genome weights length must match input count.", nameof(genome));
        }
    }

    public BrainOutput Think(BrainInput input)
    {
        ReadOnlyMemory<float> vision = input.Vision;
        int expectedInputs = vision.Length + 1;
        if (expectedInputs != _genome.InputCount)
        {
            throw new InvalidOperationException(
                $"Expected {_genome.InputCount} inputs but got {expectedInputs} (vision={vision.Length}, thirst=1)." );
        }

        float sum = _genome.BiasWeight * input.Bias;
        ReadOnlySpan<float> visionSpan = vision.Span;
        for (int i = 0; i < visionSpan.Length; i += 1)
        {
            sum += _genome.Weights[i] * visionSpan[i];
        }

        sum += _genome.Weights[visionSpan.Length] * input.Thirst01;

        float score = Sigmoid(sum);
        return new BrainOutput(score);
    }

    private static float Sigmoid(float value)
    {
        return 1f / (1f + MathF.Exp(-value));
    }
}
