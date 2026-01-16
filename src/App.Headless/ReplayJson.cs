using System.Text;
using System.Text.Json;

namespace App.Headless;

internal sealed record ReplayConfig(
    float Dt,
    int WorldWidth,
    int WorldHeight,
    int AgentVisionRays,
    float AgentVisionRange,
    float AgentFov,
    int? EmbeddingDimension,
    int? EmbeddingSeed,
    float AgentMaxSpeed,
    float AgentTurnRateRad,
    float MoveDeadzone,
    float ThirstRatePerSecond,
    float DeathGraceSeconds,
    float WaterProximityBias01,
    float ObstacleDensity01,
    int ActionPreferenceCount);

internal sealed record ReplayRecord(
    int SchemaVersion,
    int Seed,
    int Ticks,
    int Agents,
    string Brain,
    ReplayConfig Config,
    ulong Checksum,
    IReadOnlyList<ReplayTickState>? TickStates)
{
    public const int CurrentSchemaVersion = 3;
}

internal sealed record ReplayAgentState(
    float X,
    float Y,
    float HeadingRad,
    float Thirst01,
    bool Alive);

internal sealed record ReplayTickState(
    int Tick,
    ulong Checksum,
    IReadOnlyList<ReplayAgentState> Agents);

internal static class ReplayJson
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void Write(string path, ReplayRecord replay)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (replay is null)
        {
            throw new ArgumentNullException(nameof(replay));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        string json = JsonSerializer.Serialize(replay, WriteOptions);
        writer.Write(json);
    }

    public static ReplayRecord Read(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string json = File.ReadAllText(path);
        ReplayRecord? replay = JsonSerializer.Deserialize<ReplayRecord>(json, ReadOptions);

        if (replay is null)
        {
            throw new InvalidOperationException("Unable to deserialize replay.");
        }

        return replay;
    }

    public static ReplayRecord Deserialize(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        ReplayRecord? replay = JsonSerializer.Deserialize<ReplayRecord>(json, ReadOptions);
        if (replay is null)
        {
            throw new InvalidOperationException("Unable to deserialize replay.");
        }

        return replay;
    }
}
