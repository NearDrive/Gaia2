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
    ulong Checksum);
