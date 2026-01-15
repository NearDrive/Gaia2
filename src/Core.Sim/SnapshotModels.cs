using System;
using System.Collections.Generic;

namespace Core.Sim;

public sealed record SnapshotHeader
{
    public int Tick { get; init; }
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public int AgentCount { get; init; }
    public ulong WorldChecksum { get; init; }
}

public sealed record AgentSnapshot
{
    public int Id { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float DirX { get; init; }
    public float DirY { get; init; }
    public float Thirst01 { get; init; }
    public bool Alive { get; init; }
}

public sealed record WorldSnapshot
{
    public SnapshotHeader Header { get; init; } = new();
    public IReadOnlyList<AgentSnapshot> Agents { get; init; } = Array.Empty<AgentSnapshot>();
}
