namespace Core.Sim;

public sealed record BenchmarkScenarioResult(
    string Name,
    int Seed,
    int WorldSize,
    float ObstacleDensity01,
    float WaterProximityBias01,
    int Ticks,
    float ThirstRatePerSecond,
    int TicksSurvived,
    int SuccessfulDrinks,
    float AvgThirst01,
    int VisitedCells,
    double Fitness,
    ulong Checksum);
