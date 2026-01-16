using System;
using System.Collections.Generic;

namespace Persistence.Metrics;

public static class MetricsStatistics
{
    public static float ComputePercentile(IReadOnlyList<double> values, double percentile)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("Values collection cannot be empty.", nameof(values));
        }

        if (percentile < 0.0 || percentile > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile));
        }

        double[] sorted = new double[values.Count];
        for (int i = 0; i < values.Count; i += 1)
        {
            sorted[i] = values[i];
        }

        Array.Sort(sorted);

        int index = (int)Math.Floor(percentile * (sorted.Length - 1));
        return (float)sorted[index];
    }
}
