using System;
using System.Collections.Generic;
using System.IO;
using Core.Sim;
using Persistence.Metrics;
using Xunit;

namespace Persistence.Tests;

public sealed class CsvMetricsWriterTests
{
    [Fact]
    public void CsvMetricsWriter_IsDeterministic()
    {
        List<RunMetricsRow> rows = new()
        {
            new RunMetricsRow
            {
                Index = 0,
                BestFitness = 1.5f,
                AvgFitness = 1.1f,
                P50Fitness = 1.0f,
                P90Fitness = 1.4f,
                AliveAtEnd = 2,
                FinalChecksum = 42UL
            },
            new RunMetricsRow
            {
                Index = 1,
                BestFitness = 2.25f,
                AvgFitness = 2.1f,
                P50Fitness = 2.0f,
                P90Fitness = 2.2f,
                AliveAtEnd = 0,
                FinalChecksum = 99UL
            }
        };

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        try
        {
            CsvMetricsWriter.Write(tempPath, rows);
            string actual = File.ReadAllText(tempPath);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "index,bestFitness,avgFitness,p50Fitness,p90Fitness,aliveAtEnd,finalChecksum",
                "0,1.5,1.1,1,1.4,2,42",
                "1,2.25,2.1,2,2.2,0,99"
            }) + Environment.NewLine;

            Assert.Equal(expected, actual);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Percentile_Computation_IsStable()
    {
        double[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        float p50 = MetricsStatistics.ComputePercentile(values, 0.50);
        float p90 = MetricsStatistics.ComputePercentile(values, 0.90);

        Assert.Equal(5f, p50);
        Assert.Equal(9f, p90);
    }
}
