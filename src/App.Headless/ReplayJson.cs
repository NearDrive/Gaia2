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
    float AgentMaxSpeed,
    float MoveDeadzone,
    float ThirstRatePerSecond,
    float DeathGraceSeconds,
    float WaterProximityBias01,
    float ObstacleDensity01);

internal sealed record ReplayRecord(
    int SchemaVersion,
    int Seed,
    int Ticks,
    int Agents,
    string Brain,
    ReplayConfig Config,
    ulong Checksum)
{
    public const int CurrentSchemaVersion = 1;
}

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
}
