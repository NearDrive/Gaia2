using System.Text;
using System.Text.Json;

namespace Core.Sim;

public static class ReplayJson
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Replay Read(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string json = File.ReadAllText(path);
        return Deserialize(json);
    }

    public static Replay Deserialize(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        Replay? replay = JsonSerializer.Deserialize<Replay>(json, ReadOptions);

        if (replay is null)
        {
            throw new InvalidOperationException("Unable to deserialize replay.");
        }

        return replay;
    }

    public static void Write(string path, Replay replay)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (replay is null)
        {
            throw new ArgumentNullException(nameof(replay));
        }

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
        WriteReplay(writer, replay);
        writer.Flush();
    }

    public static string Serialize(Replay replay)
    {
        if (replay is null)
        {
            throw new ArgumentNullException(nameof(replay));
        }

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
        WriteReplay(writer, replay);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteReplay(Utf8JsonWriter writer, Replay replay)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("header");
        WriteHeader(writer, replay.Header);

        writer.WritePropertyName("ticks");
        writer.WriteStartArray();
        foreach (ReplayTick tick in replay.Ticks)
        {
            WriteTick(writer, tick);
        }

        writer.WriteEndArray();
        writer.WriteNumber("finalChecksum", replay.FinalChecksum);
        writer.WriteEndObject();
    }

    private static void WriteHeader(Utf8JsonWriter writer, ReplayHeader header)
    {
        writer.WriteStartObject();
        writer.WriteNumber("seed", header.Seed);
        writer.WritePropertyName("scenario");
        WriteScenario(writer, header.Scenario);
        writer.WriteNumber("worldWidth", header.WorldWidth);
        writer.WriteNumber("worldHeight", header.WorldHeight);
        writer.WriteNumber("dt", header.Dt);
        writer.WriteNumber("ticks", header.Ticks);
        writer.WriteNumber("worldChecksum", header.WorldChecksum);
        writer.WriteString("brainInfo", header.BrainInfo ?? string.Empty);
        writer.WriteEndObject();
    }

    private static void WriteScenario(Utf8JsonWriter writer, Scenario? scenario)
    {
        if (scenario is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("name", scenario.Name);
        writer.WriteNumber("seed", scenario.Seed);
        writer.WriteNumber("worldSize", scenario.WorldSize);
        writer.WriteNumber("obstacleDensity01", scenario.ObstacleDensity01);
        writer.WriteNumber("waterProximityBias01", scenario.WaterProximityBias01);
        writer.WriteNumber("ticks", scenario.Ticks);
        writer.WriteNumber("thirstRatePerSecond", scenario.ThirstRatePerSecond);
        writer.WriteEndObject();
    }

    private static void WriteTick(Utf8JsonWriter writer, ReplayTick tick)
    {
        writer.WriteStartObject();
        writer.WriteNumber("tick", tick.Tick);
        writer.WriteNumber("ax", tick.Ax);
        writer.WriteNumber("ay", tick.Ay);
        writer.WriteNumber("thirst01", tick.Thirst01);
        writer.WriteBoolean("alive", tick.Alive);
        writer.WriteNumber("outMoveX", tick.OutMoveX);
        writer.WriteNumber("outMoveY", tick.OutMoveY);
        writer.WriteNumber("outDrink", tick.OutDrink);
        writer.WriteNumber("successfulDrinksTotal", tick.SuccessfulDrinksTotal);
        writer.WriteNumber("visitedCells", tick.VisitedCells);
        writer.WriteEndObject();
    }
}
