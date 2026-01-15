namespace Core.Sim;

public sealed record Scenario
{
    public string Name { get; init; } = string.Empty;
    public int Seed { get; init; }
    public int WorldSize { get; init; }
    public float ObstacleDensity01 { get; init; }
    public float WaterProximityBias01 { get; init; }
    public int Ticks { get; init; }
    public float ThirstRatePerSecond { get; init; }
}
