using System.Globalization;
using System.Text;
using Core.Sim;

namespace App.Headless;

internal static class BenchmarkCsvWriter
{
    internal const string Header =
        "scenario_name,seed,world_size,obstacle_density,water_bias,ticks," +
        "ticks_survived,drinks,avg_thirst,visited_cells,fitness,checksum";

    public static void Write(string path, BenchmarkReport report)
    {
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine(Header);

        foreach (BenchmarkScenarioResult scenario in report.Scenarios)
        {
            string line = string.Join(
                ",",
                scenario.Name,
                scenario.Seed.ToString(CultureInfo.InvariantCulture),
                scenario.WorldSize.ToString(CultureInfo.InvariantCulture),
                scenario.ObstacleDensity01.ToString("0.000", CultureInfo.InvariantCulture),
                scenario.WaterProximityBias01.ToString("0.000", CultureInfo.InvariantCulture),
                scenario.Ticks.ToString(CultureInfo.InvariantCulture),
                scenario.TicksSurvived.ToString(CultureInfo.InvariantCulture),
                scenario.SuccessfulDrinks.ToString(CultureInfo.InvariantCulture),
                scenario.AvgThirst01.ToString("0.000000", CultureInfo.InvariantCulture),
                scenario.VisitedCells.ToString(CultureInfo.InvariantCulture),
                scenario.Fitness.ToString("0.000000", CultureInfo.InvariantCulture),
                scenario.Checksum.ToString(CultureInfo.InvariantCulture));

            writer.WriteLine(line);
        }
    }
}
