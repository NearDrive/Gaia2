using System;
using System.Collections.Generic;

namespace Core.Sim;

public readonly record struct EpisodeResult(
    int Seed,
    int TicksRequested,
    int TicksSurvived,
    int SuccessfulDrinks,
    float AvgThirst01,
    double DistanceTraveled,
    int VisitedCells,
    int DrinkableCellsVisited,
    double ExplorationScore,
    double DrinkableExplorationScore,
    double Fitness,
    ulong Checksum,
    IReadOnlyList<int>? SnapshotTicksWritten = null)
{
    public IReadOnlyList<WorldSnapshot> Snapshots { get; init; } = Array.Empty<WorldSnapshot>();
    public bool SnapshotsEnabled { get; init; }
    public int SnapshotEveryNTicks { get; init; }
    public int SnapshotCount { get; init; }
}
