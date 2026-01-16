using System;
using System.Collections.Generic;
using System.IO;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class EmbeddingRegistryTests
{
    [Fact]
    public void SameTile_SameEmbedding()
    {
        EmbeddingRegistry registry = new(dimension: 8, seed: 42);

        float[] first = registry.GetTileEmbedding(7);
        float[] second = registry.GetTileEmbedding(7);

        Assert.Equal(first, second);
    }

    [Fact]
    public void OrderIndependent()
    {
        EmbeddingRegistry first = new(dimension: 8, seed: 101);
        EmbeddingRegistry second = new(dimension: 8, seed: 101);

        first.GetTileEmbedding(1);
        first.GetTileEmbedding(2);
        float[] firstEmbedding = first.GetTileEmbedding(3);
        first.GetTileEmbedding(4);

        second.GetTileEmbedding(4);
        float[] secondEmbedding = second.GetTileEmbedding(3);
        second.GetTileEmbedding(2);
        second.GetTileEmbedding(1);

        Assert.Equal(firstEmbedding, secondEmbedding);
    }

    [Fact]
    public void SaveLoad_Roundtrip()
    {
        EmbeddingRegistry registry = new(dimension: 8, seed: 77);
        int[] tileIds = { 2, 5, 9 };
        Dictionary<int, float[]> embeddings = new();

        foreach (int tileId in tileIds)
        {
            embeddings[tileId] = registry.GetTileEmbedding(tileId);
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "gaia_embeddings", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(tempDir, "embedding_registry.json");

        try
        {
            registry.Save(path);

            EmbeddingRegistry loaded = EmbeddingRegistry.Load(path);

            foreach (int tileId in tileIds)
            {
                float[] reloaded = loaded.GetTileEmbedding(tileId);
                Assert.Equal(embeddings[tileId], reloaded);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
