namespace Core.Sim;

public sealed class BenchmarkRunner
{
    private readonly SimulationConfig _baseConfig;

    public BenchmarkRunner(SimulationConfig baseConfig)
    {
        _baseConfig = baseConfig;
    }

    public BenchmarkReport Run(IBrain brain, IReadOnlyList<Scenario> scenarios)
    {
        if (brain is null)
        {
            throw new ArgumentNullException(nameof(brain));
        }

        if (scenarios is null)
        {
            throw new ArgumentNullException(nameof(scenarios));
        }

        List<BenchmarkScenarioResult> results = new(scenarios.Count);

        foreach (Scenario scenario in scenarios)
        {
            SimulationConfig scenarioConfig = _baseConfig with
            {
                WorldWidth = scenario.WorldSize,
                WorldHeight = scenario.WorldSize,
                ObstacleDensity01 = scenario.ObstacleDensity01,
                WaterProximityBias01 = scenario.WaterProximityBias01,
                ThirstRatePerSecond = scenario.ThirstRatePerSecond
            };

            EpisodeRunner runner = new(scenarioConfig);
            EpisodeResult result = runner.RunEpisode(brain, scenario.Seed, scenario.Ticks, 1);

            results.Add(new BenchmarkScenarioResult(
                scenario.Name,
                scenario.Seed,
                scenario.WorldSize,
                scenario.ObstacleDensity01,
                scenario.WaterProximityBias01,
                scenario.Ticks,
                scenario.ThirstRatePerSecond,
                result.TicksSurvived,
                result.SuccessfulDrinks,
                Quantize(result.AvgThirst01, 6),
                result.VisitedCells,
                Quantize(result.Fitness, 6),
                result.Checksum));
        }

        double meanSurvived = results.Count > 0
            ? results.Average(result => result.TicksSurvived)
            : 0.0;
        double successRate = results.Count > 0
            ? results.Count(result => result.TicksSurvived >= result.Ticks) / (double)results.Count
            : 0.0;
        double meanFitness = results.Count > 0
            ? results.Average(result => result.Fitness)
            : 0.0;
        BenchmarkScenarioResult? worst = results.Count > 0
            ? results.OrderBy(result => result.Fitness).First()
            : null;

        BenchmarkSummary summary = new(
            MeanSurvivedTicks: Quantize(meanSurvived, 6),
            SuccessRate: Quantize(successRate, 6),
            MeanFitness: Quantize(meanFitness, 6),
            WorstScenario: worst?.Name ?? string.Empty);

        return new BenchmarkReport(results, summary);
    }

    private static double Quantize(double value, int decimals)
    {
        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    private static float Quantize(float value, int decimals)
    {
        return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
}
