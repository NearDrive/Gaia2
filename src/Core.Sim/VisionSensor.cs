using System.Numerics;

namespace Core.Sim;

public sealed class VisionSensor
{
    private const float StepSize = 0.25f;
    private const float QuantizeScale = 1000f;
    private readonly EmbeddingRegistry _embeddingRegistry;

    public VisionSensor(int rayCount, float maxDistance, float fovRadians, EmbeddingRegistry embeddingRegistry)
    {
        if (rayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rayCount));
        }

        if (maxDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistance));
        }

        if (fovRadians < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(fovRadians));
        }

        if (embeddingRegistry is null)
        {
            throw new ArgumentNullException(nameof(embeddingRegistry));
        }

        RayCount = rayCount;
        MaxDistance = maxDistance;
        FovRadians = fovRadians;
        _embeddingRegistry = embeddingRegistry;
    }

    public int RayCount { get; }

    public float MaxDistance { get; }

    public float FovRadians { get; }

    public int EmbeddingDimension => _embeddingRegistry.Dimension;

    public int OutputLength => RayCount * (EmbeddingDimension + 1);

    public float[] Sense(GridWorld world, Vector2 agentPos, float agentFacingRadians)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        float[] results = new float[OutputLength];
        Sense(world, agentPos, agentFacingRadians, results);
        return results;
    }

    public void Sense(GridWorld world, Vector2 agentPos, float agentFacingRadians, float[] results)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (results is null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        if (results.Length < OutputLength)
        {
            throw new ArgumentException("Results buffer is too small.", nameof(results));
        }

        float startAngle = agentFacingRadians - (FovRadians * 0.5f);
        float angleStep = RayCount > 1 ? FovRadians / (RayCount - 1) : 0f;

        for (int i = 0; i < RayCount; i += 1)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            float distanceNorm = 1f;
            int baseOffset = i * (EmbeddingDimension + 1);
            Array.Clear(results, baseOffset + 1, EmbeddingDimension);

            for (float distance = StepSize; distance <= MaxDistance; distance += StepSize)
            {
                Vector2 sample = agentPos + (direction * distance);
                int tileX = (int)MathF.Floor(sample.X);
                int tileY = (int)MathF.Floor(sample.Y);

                TileId hitId = TileId.Empty;
                bool hit = false;

                if (!world.InBounds(tileX, tileY))
                {
                    hit = true;
                    hitId = TileId.Solid;
                }
                else
                {
                    Tile tile = world.GetTile(tileX, tileY);
                    if (tile.Id == TileId.Solid || tile.Id == TileId.Water)
                    {
                        hit = true;
                        hitId = tile.Id;
                    }
                }

                if (hit)
                {
                    distanceNorm = Quantize(distance / MaxDistance);
                    float[] embedding = _embeddingRegistry.GetTileEmbedding((int)hitId);
                    Array.Copy(embedding, 0, results, baseOffset + 1, EmbeddingDimension);
                    break;
                }
            }

            results[baseOffset] = distanceNorm;
        }
    }

    private static float Quantize(float value)
    {
        return MathF.Round(value * QuantizeScale) / QuantizeScale;
    }
}
