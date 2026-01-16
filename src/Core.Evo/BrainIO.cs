using System;

namespace Core.Evo;

public static class BrainIO
{
    public const int NonVisionInputCount = 2;

    public static int OutputCount(int actionPreferenceCount)
    {
        if (actionPreferenceCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actionPreferenceCount));
        }

        return 2 + actionPreferenceCount;
    }

    public static int VisionInputCount(int rayCount, int embeddingDimension)
    {
        if (rayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rayCount));
        }

        if (embeddingDimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(embeddingDimension));
        }

        return rayCount * (1 + embeddingDimension);
    }

    public static int InputCount(int rayCount, int embeddingDimension)
    {
        return VisionInputCount(rayCount, embeddingDimension) + NonVisionInputCount;
    }
}

public readonly record struct BrainInput(
    float[] Vision,
    float Thirst01,
    float Bias);

public readonly record struct BrainOutput
{
    public float RotationDelta { get; init; }
    public float ForwardSpeed { get; init; }
    public float[] ActionPreferenceVector { get; init; } = Array.Empty<float>();
}
