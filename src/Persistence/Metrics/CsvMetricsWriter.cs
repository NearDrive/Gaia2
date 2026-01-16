using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Sim;

namespace Persistence.Metrics;

public static class CsvMetricsWriter
{
    private const string Header = "index,bestFitness,avgFitness,p50Fitness,p90Fitness,aliveAtEnd,finalChecksum";

    public static void Write(string path, IReadOnlyList<RunMetricsRow> rows)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine(Header);

        foreach (RunMetricsRow row in rows)
        {
            string line = string.Join(
                ",",
                row.Index.ToString(CultureInfo.InvariantCulture),
                row.BestFitness.ToString(CultureInfo.InvariantCulture),
                row.AvgFitness.ToString(CultureInfo.InvariantCulture),
                row.P50Fitness.ToString(CultureInfo.InvariantCulture),
                row.P90Fitness.ToString(CultureInfo.InvariantCulture),
                row.AliveAtEnd.ToString(CultureInfo.InvariantCulture),
                row.FinalChecksum.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(line);
        }
    }
}
