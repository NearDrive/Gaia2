namespace Core.Sim;

public sealed record BenchmarkReport(
    IReadOnlyList<BenchmarkScenarioResult> Scenarios,
    BenchmarkSummary Summary);

public sealed record BenchmarkSummary(
    double MeanSurvivedTicks,
    double SuccessRate,
    double MeanFitness,
    string WorstScenario);
