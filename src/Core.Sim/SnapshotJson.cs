using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Sim;

public static class SnapshotJson
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.Strict
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.Strict
    };

    public static string Serialize(WorldSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return JsonSerializer.Serialize(snapshot, WriteOptions);
    }

    public static WorldSnapshot Deserialize(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        WorldSnapshot? snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, ReadOptions);

        if (snapshot is null)
        {
            throw new InvalidOperationException("Unable to deserialize snapshot.");
        }

        return snapshot;
    }

    public static void Write(string path, WorldSnapshot snapshot)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string json = Serialize(snapshot);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(json);
    }

    public static void WriteSnapshots(string directory, IReadOnlyList<WorldSnapshot> snapshots)
    {
        if (directory is null)
        {
            throw new ArgumentNullException(nameof(directory));
        }

        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        Directory.CreateDirectory(directory);

        foreach (WorldSnapshot snapshot in snapshots)
        {
            string tick = snapshot.Header.Tick.ToString(CultureInfo.InvariantCulture);
            string fileName = $"snap_{tick}.json";
            string path = Path.Combine(directory, fileName);
            Write(path, snapshot);
        }
    }
}
