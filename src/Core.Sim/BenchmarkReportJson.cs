using System.Text.Json;

namespace Core.Sim;

public static class BenchmarkReportJson
{
    public static void Write(string path, BenchmarkReport report)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
        WriteReport(writer, report);
        writer.Flush();
    }

    private static void WriteReport(Utf8JsonWriter writer, BenchmarkReport report)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("summary");
        writer.WriteStartObject();
        writer.WriteNumber("meanSurvivedTicks", report.Summary.MeanSurvivedTicks);
        writer.WriteNumber("successRate", report.Summary.SuccessRate);
        writer.WriteNumber("meanFitness", report.Summary.MeanFitness);
        writer.WriteString("worstScenario", report.Summary.WorstScenario);
        writer.WriteEndObject();

        writer.WritePropertyName("scenarios");
        writer.WriteStartArray();

        foreach (BenchmarkScenarioResult scenario in report.Scenarios)
        {
            writer.WriteStartObject();
            writer.WriteString("name", scenario.Name);
            writer.WriteNumber("seed", scenario.Seed);
            writer.WriteNumber("worldSize", scenario.WorldSize);
            writer.WriteNumber("obstacleDensity01", scenario.ObstacleDensity01);
            writer.WriteNumber("waterProximityBias01", scenario.WaterProximityBias01);
            writer.WriteNumber("ticks", scenario.Ticks);
            writer.WriteNumber("thirstRatePerSecond", scenario.ThirstRatePerSecond);
            writer.WriteNumber("ticksSurvived", scenario.TicksSurvived);
            writer.WriteNumber("successfulDrinks", scenario.SuccessfulDrinks);
            writer.WriteNumber("avgThirst01", scenario.AvgThirst01);
            writer.WriteNumber("visitedCells", scenario.VisitedCells);
            writer.WriteNumber("fitness", scenario.Fitness);
            writer.WriteNumber("checksum", scenario.Checksum);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
