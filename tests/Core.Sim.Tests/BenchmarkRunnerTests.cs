using Core.Evo;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class BenchmarkRunnerTests
{
    [Fact]
    public void Benchmark_IsDeterministic_ForSameGenome()
    {
        SimulationConfig baseConfig = CreateMiniSimConfig();
        BenchmarkRunner runner = new(baseConfig);
        IBrain brain = new FixedBrain();
        IReadOnlyList<Scenario> scenarios = new[]
        {
            new Scenario
            {
                Name = "alpha",
                Seed = 11,
                WorldSize = 8,
                ObstacleDensity01 = 0.1f,
                WaterProximityBias01 = 0.6f,
                Ticks = 30,
                ThirstRatePerSecond = 0.01f
            },
            new Scenario
            {
                Name = "beta",
                Seed = 12,
                WorldSize = 10,
                ObstacleDensity01 = 0.2f,
                WaterProximityBias01 = 0.2f,
                Ticks = 30,
                ThirstRatePerSecond = 0.012f
            }
        };

        BenchmarkReport first = runner.Run(brain, scenarios);
        BenchmarkReport second = runner.Run(brain, scenarios);

        Assert.Equal(first.Scenarios.Count, second.Scenarios.Count);
        for (int i = 0; i < first.Scenarios.Count; i += 1)
        {
            BenchmarkScenarioResult a = first.Scenarios[i];
            BenchmarkScenarioResult b = second.Scenarios[i];

            Assert.Equal(a.Name, b.Name);
            Assert.Equal(a.Seed, b.Seed);
            Assert.Equal(a.WorldSize, b.WorldSize);
            Assert.Equal(a.ObstacleDensity01, b.ObstacleDensity01);
            Assert.Equal(a.WaterProximityBias01, b.WaterProximityBias01);
            Assert.Equal(a.Ticks, b.Ticks);
            Assert.Equal(a.ThirstRatePerSecond, b.ThirstRatePerSecond);
            Assert.Equal(a.TicksSurvived, b.TicksSurvived);
            Assert.Equal(a.SuccessfulDrinks, b.SuccessfulDrinks);
            Assert.Equal(a.AvgThirst01, b.AvgThirst01);
            Assert.Equal(a.VisitedCells, b.VisitedCells);
            Assert.Equal(a.Fitness, b.Fitness);
            Assert.Equal(a.Checksum, b.Checksum);
        }

        Assert.Equal(first.Summary.MeanSurvivedTicks, second.Summary.MeanSurvivedTicks);
        Assert.Equal(first.Summary.SuccessRate, second.Summary.SuccessRate);
        Assert.Equal(first.Summary.MeanFitness, second.Summary.MeanFitness);
        Assert.Equal(first.Summary.WorstScenario, second.Summary.WorstScenario);
    }

    [Fact]
    public void ScenarioList_IsStable()
    {
        string[] expectedNames =
        {
            "easy_near_water_small",
            "easy_near_water",
            "medium_water_far",
            "medium_obstacles",
            "dense_obstacles",
            "large_world_sparse",
            "large_world_dense",
            "dry_world",
            "fast_thirst",
            "neutral_medium"
        };

        IReadOnlyList<Scenario> scenarios = BenchmarkScenarios.Default;
        Assert.Equal(expectedNames.Length, scenarios.Count);

        for (int i = 0; i < expectedNames.Length; i += 1)
        {
            Assert.Equal(expectedNames[i], scenarios[i].Name);
        }
    }

    private static SimulationConfig CreateMiniSimConfig()
    {
        return new SimulationConfig(
            seed: 1,
            dt: 1f,
            ticksPerEpisode: 30,
            worldWidth: 12,
            worldHeight: 12,
            agentVisionRays: 4,
            agentVisionRange: 5f,
            agentFov: MathF.PI / 2f);
    }

    private sealed class FixedBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput
            {
                MoveX = 0f,
                MoveY = 0f,
                ActionDrinkScore = 0f
            };
        }
    }
}
