namespace Core.Sim;

public sealed record ReplayHeader
{
    public int Seed { get; init; }
    public Scenario? Scenario { get; init; }
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public float Dt { get; init; }
    public int Ticks { get; init; }
    public ulong WorldChecksum { get; init; }
    public string BrainInfo { get; init; } = string.Empty;
}

public sealed record ReplayTick
{
    public int Tick { get; init; }
    public float Ax { get; init; }
    public float Ay { get; init; }
    public float Thirst01 { get; init; }
    public bool Alive { get; init; }
    public float OutMoveX { get; init; }
    public float OutMoveY { get; init; }
    public float OutDrink { get; init; }
    public int SuccessfulDrinksTotal { get; init; }
    public int VisitedCells { get; init; }
}

public sealed record Replay
{
    public ReplayHeader Header { get; init; } = new();
    public List<ReplayTick> Ticks { get; init; } = new();
    public ulong FinalChecksum { get; init; }
}
