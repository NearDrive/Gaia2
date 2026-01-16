using System.Text.Json;
using System;
using System.IO;

namespace Core.Sim;

public static class SnapshotStreamWriter
{
    public static void WriteHeader(TextWriter writer, SnapshotStreamHeader header, JsonSerializerOptions options)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (header is null)
        {
            throw new ArgumentNullException(nameof(header));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        string json = JsonSerializer.Serialize(header, options);
        writer.WriteLine(json);
    }

    public static void WriteSnapshot(TextWriter writer, WorldSnapshot snapshot, JsonSerializerOptions options)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        string json = JsonSerializer.Serialize(snapshot, options);
        writer.WriteLine(json);
    }
}

public sealed record SnapshotStreamHeader
{
    public int Version { get; init; } = 1;
    public int Seed { get; init; }
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public float Dt { get; init; }
    public int SnapshotEveryNTicks { get; init; }
}
