namespace Core.Sim;

public static class BenchmarkScenarios
{
    public static IReadOnlyList<Scenario> Default { get; } = new[]
    {
        new Scenario
        {
            Name = "easy_near_water_small",
            Seed = 1101,
            WorldSize = 20,
            ObstacleDensity01 = 0.05f,
            WaterProximityBias01 = 0.85f,
            Ticks = 2000,
            ThirstRatePerSecond = 0.01f
        },
        new Scenario
        {
            Name = "easy_near_water",
            Seed = 1102,
            WorldSize = 28,
            ObstacleDensity01 = 0.08f,
            WaterProximityBias01 = 0.7f,
            Ticks = 2500,
            ThirstRatePerSecond = 0.01f
        },
        new Scenario
        {
            Name = "medium_water_far",
            Seed = 2101,
            WorldSize = 32,
            ObstacleDensity01 = 0.12f,
            WaterProximityBias01 = 0.25f,
            Ticks = 3000,
            ThirstRatePerSecond = 0.011f
        },
        new Scenario
        {
            Name = "medium_obstacles",
            Seed = 2102,
            WorldSize = 32,
            ObstacleDensity01 = 0.2f,
            WaterProximityBias01 = 0.2f,
            Ticks = 3000,
            ThirstRatePerSecond = 0.011f
        },
        new Scenario
        {
            Name = "dense_obstacles",
            Seed = 3101,
            WorldSize = 28,
            ObstacleDensity01 = 0.35f,
            WaterProximityBias01 = 0.15f,
            Ticks = 3000,
            ThirstRatePerSecond = 0.0115f
        },
        new Scenario
        {
            Name = "large_world_sparse",
            Seed = 4101,
            WorldSize = 48,
            ObstacleDensity01 = 0.08f,
            WaterProximityBias01 = 0.2f,
            Ticks = 3500,
            ThirstRatePerSecond = 0.012f
        },
        new Scenario
        {
            Name = "large_world_dense",
            Seed = 4102,
            WorldSize = 52,
            ObstacleDensity01 = 0.22f,
            WaterProximityBias01 = 0.15f,
            Ticks = 3500,
            ThirstRatePerSecond = 0.012f
        },
        new Scenario
        {
            Name = "dry_world",
            Seed = 5101,
            WorldSize = 32,
            ObstacleDensity01 = 0.1f,
            WaterProximityBias01 = 0f,
            Ticks = 3000,
            ThirstRatePerSecond = 0.012f
        },
        new Scenario
        {
            Name = "fast_thirst",
            Seed = 5102,
            WorldSize = 30,
            ObstacleDensity01 = 0.15f,
            WaterProximityBias01 = 0.2f,
            Ticks = 2800,
            ThirstRatePerSecond = 0.015f
        },
        new Scenario
        {
            Name = "neutral_medium",
            Seed = 6101,
            WorldSize = 36,
            ObstacleDensity01 = 0.18f,
            WaterProximityBias01 = 0.1f,
            Ticks = 3200,
            ThirstRatePerSecond = 0.011f
        }
    };
}
