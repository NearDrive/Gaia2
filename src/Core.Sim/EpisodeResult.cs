namespace Core.Sim;

public readonly record struct EpisodeResult(
    int Seed,
    int TicksRequested,
    int TicksSurvived,
    int SuccessfulDrinks,
    float AvgThirst01,
    double Fitness,
    ulong Checksum);
