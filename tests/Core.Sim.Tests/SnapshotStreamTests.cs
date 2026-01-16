using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class SnapshotStreamTests
{
    [Fact]
    public void SnapshotStream_Roundtrip_ReadsTicks()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new NoOpBrain();
        const int ticks = 60;
        const int snapshotEvery = 10;

        EpisodeResult result = runner.RunEpisode(
            brain,
            123,
            ticks,
            2,
            captureSnapshots: true,
            snapshotEveryNTicks: snapshotEvery,
            writeSnapshotsToDisk: false,
            stopEarlyWhenAllAgentsDead: false);

        string jsonl = WriteStream(config, result, snapshotEvery);
        using StringReader reader = new(jsonl);
        string? headerLine = reader.ReadLine();

        Assert.False(string.IsNullOrWhiteSpace(headerLine));

        JsonSerializerOptions readOptions = SnapshotJson.GetReadOptions();
        SnapshotStreamHeader? header = JsonSerializer.Deserialize<SnapshotStreamHeader>(headerLine, readOptions);

        Assert.NotNull(header);

        var snapshots = ReadSnapshots(reader);
        int[] ticksRead = snapshots.Select(snapshot => snapshot.Header.Tick).ToArray();

        Assert.Equal(new[] { 0, 10, 20, 30, 40, 50 }, ticksRead);
        Assert.Equal(6, snapshots.Count);
    }

    [Fact]
    public void SnapshotStream_IsDeterministic_SameRunSameText()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new NoOpBrain();
        const int ticks = 60;
        const int snapshotEvery = 10;
        const int seed = 999;

        string first = WriteStream(
            config,
            runner.RunEpisode(
                brain,
                seed,
                ticks,
                2,
                captureSnapshots: true,
                snapshotEveryNTicks: snapshotEvery,
                writeSnapshotsToDisk: false,
                stopEarlyWhenAllAgentsDead: false),
            snapshotEvery);

        string second = WriteStream(
            config,
            runner.RunEpisode(
                brain,
                seed,
                ticks,
                2,
                captureSnapshots: true,
                snapshotEveryNTicks: snapshotEvery,
                writeSnapshotsToDisk: false,
                stopEarlyWhenAllAgentsDead: false),
            snapshotEvery);

        Assert.Equal(first, second);
    }

    private static string WriteStream(SimulationConfig config, EpisodeResult result, int snapshotEvery)
    {
        SnapshotStreamHeader header = new()
        {
            Seed = result.Seed,
            WorldWidth = config.WorldWidth,
            WorldHeight = config.WorldHeight,
            Dt = config.Dt,
            SnapshotEveryNTicks = snapshotEvery
        };

        using StringWriter writer = new();
        JsonSerializerOptions writeOptions = SnapshotJson.GetWriteOptions();

        SnapshotStreamWriter.WriteHeader(writer, header, writeOptions);

        foreach (WorldSnapshot snapshot in result.Snapshots)
        {
            SnapshotStreamWriter.WriteSnapshot(writer, snapshot, writeOptions);
        }

        return writer.ToString();
    }

    private static IReadOnlyList<WorldSnapshot> ReadSnapshots(StringReader reader)
    {
        List<WorldSnapshot> snapshots = new();

        while (true)
        {
            string? line = reader.ReadLine();

            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                continue;
            }

            snapshots.Add(SnapshotJson.Deserialize(line));
        }

        return snapshots.OrderBy(snapshot => snapshot.Header.Tick).ToList();
    }

    private static SimulationConfig CreateConfig(float thirstRate = 0.05f, float deathGrace = 2f)
    {
        return new SimulationConfig(
            seed: 0,
            dt: 1f,
            ticksPerEpisode: 100,
            worldWidth: 8,
            worldHeight: 8,
            agentVisionRays: 4,
            agentVisionRange: 4f,
            agentFov: MathF.PI / 2f,
            agentMaxSpeed: 1.5f,
            moveDeadzone: 0.05f,
            thirstRatePerSecond: thirstRate,
            deathGraceSeconds: deathGrace);
    }
}
