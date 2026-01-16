using System;

namespace Core.Evo;

public static class BrainIO
{
    public const int NonVisionInputCount = 2;
    public const int OutputCount = 3;

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
    public float MoveX { get; init; }
    public float MoveY { get; init; }
    public float ActionDrinkScore { get; init; }
}
