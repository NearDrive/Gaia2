using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Core.Sim;

public sealed class EmbeddingRegistry
{
    private readonly Dictionary<int, float[]> _tileEmbeddings;

    public int Dimension { get; }
    public int Seed { get; }

    public EmbeddingRegistry(int dimension, int seed)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be > 0.");
        }

        Dimension = dimension;
        Seed = seed;
        _tileEmbeddings = new Dictionary<int, float[]>();
    }

    private EmbeddingRegistry(int dimension, int seed, Dictionary<int, float[]> tiles)
    {
        Dimension = dimension;
        Seed = seed;
        _tileEmbeddings = tiles;
    }

    public float[] GetTileEmbedding(int tileId)
    {
        if (_tileEmbeddings.TryGetValue(tileId, out float[]? embedding))
        {
            return embedding;
        }

        embedding = GenerateEmbedding(tileId);
        _tileEmbeddings[tileId] = embedding;
        return embedding;
    }

    public static EmbeddingRegistry Load(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string json = File.ReadAllText(path, Encoding.UTF8);
        EmbeddingRegistryFile? file = JsonSerializer.Deserialize<EmbeddingRegistryFile>(json, SnapshotJson.GetReadOptions());

        if (file is null)
        {
            throw new InvalidOperationException("Unable to deserialize embedding registry.");
        }

        if (file.Dimension <= 0)
        {
            throw new InvalidOperationException("Embedding dimension must be > 0.");
        }

        Dictionary<int, float[]> tiles = new(file.Tiles.Count);
        foreach (KeyValuePair<int, float[]> pair in file.Tiles)
        {
            if (pair.Value.Length != file.Dimension)
            {
                throw new InvalidOperationException("Embedding dimension mismatch in registry file.");
            }

            tiles[pair.Key] = pair.Value;
        }

        return new EmbeddingRegistry(file.Dimension, file.Seed, tiles);
    }

    public void Save(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        Dictionary<int, float[]> tiles = new(_tileEmbeddings.Count);
        foreach (KeyValuePair<int, float[]> pair in _tileEmbeddings.OrderBy(pair => pair.Key))
        {
            tiles[pair.Key] = pair.Value;
        }

        EmbeddingRegistryFile file = new()
        {
            Dimension = Dimension,
            Seed = Seed,
            Tiles = tiles
        };

        string json = JsonSerializer.Serialize(file, SnapshotJson.GetWriteOptions());
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(json);
    }

    private float[] GenerateEmbedding(int tileId)
    {
        int derivedSeed = DeriveSeed(Seed, tileId);
        SimRng rng = new(derivedSeed);

        float[] embedding = new float[Dimension];
        for (int i = 0; i < Dimension; i += 1)
        {
            // Deterministic range for embeddings: [-1, 1].
            embedding[i] = rng.NextFloat(-1f, 1f);
        }

        return embedding;
    }

    private static int DeriveSeed(int seed, int tileId)
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + seed;
            hash = (hash * 31) + tileId;
            return hash;
        }
    }
}

public sealed record EmbeddingRegistryFile
{
    public int Version { get; init; } = 1;
    public int Dimension { get; init; }
    public int Seed { get; init; }
    public Dictionary<int, float[]> Tiles { get; init; } = new();
}
