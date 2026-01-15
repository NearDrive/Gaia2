namespace Core.Evo;

public readonly record struct BrainInput(
    float[] Vision,
    float Thirst01,
    float Bias);

public readonly record struct BrainOutput
{
    public float MoveX;
    public float MoveY;
    public float ActionDrinkScore;
}
