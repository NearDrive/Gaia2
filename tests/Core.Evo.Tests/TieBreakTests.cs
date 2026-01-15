using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Core.Evo.Tests;

public class TieBreakTests
{
    [Fact]
    public void TieBreak_IsDeterministic_WhenFitnessEqual()
    {
        List<GenomeFitnessScore> scores = new()
        {
            new GenomeFitnessScore(Index: 2, FitnessAdjusted: 10, FitnessRaw: 12, NodeCount: 5, ConnectionCount: 5),
            new GenomeFitnessScore(Index: 1, FitnessAdjusted: 10, FitnessRaw: 12, NodeCount: 3, ConnectionCount: 3),
            new GenomeFitnessScore(Index: 0, FitnessAdjusted: 10, FitnessRaw: 12, NodeCount: 3, ConnectionCount: 3)
        };

        List<GenomeFitnessScore> firstOrder = scores.OrderBy(score => score, Comparer<GenomeFitnessScore>.Create(GenomeFitnessScore.Compare)).ToList();
        List<GenomeFitnessScore> secondOrder = scores.OrderBy(score => score, Comparer<GenomeFitnessScore>.Create(GenomeFitnessScore.Compare)).ToList();

        int[] expectedOrder = { 0, 1, 2 };
        Assert.Equal(expectedOrder, firstOrder.Select(score => score.Index));
        Assert.Equal(expectedOrder, secondOrder.Select(score => score.Index));
    }
}
