using System.Numerics;

namespace Core.Sim;

public sealed class VisionSensor
{
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
            int baseOffset = i * (EmbeddingDimension + 1);
            Array.Clear(results, baseOffset + 1, EmbeddingDimension);

            RaycastHit hit = RaycastDda(world, agentPos, direction, MaxDistance);
            float distanceNorm = hit.Hit
                ? Quantize(Math.Clamp(hit.Distance / MaxDistance, 0f, 1f))
                : 1f;

            TileId hitId = hit.Hit ? hit.TileId : TileId.Empty;
            float[] embedding = _embeddingRegistry.GetTileEmbedding((int)hitId);
            Array.Copy(embedding, 0, results, baseOffset + 1, EmbeddingDimension);
            results[baseOffset] = distanceNorm;
        }
    }

    internal static RaycastHit RaycastDda(GridWorld world, Vector2 origin, Vector2 direction, float maxDistance)
    {
        if (maxDistance <= 0f)
        {
            return new RaycastHit(TileId.Empty, 0f, hit: false);
        }

        if (direction == Vector2.Zero)
        {
            return new RaycastHit(TileId.Empty, 0f, hit: false);
        }

        int cellX = (int)MathF.Floor(origin.X);
        int cellY = (int)MathF.Floor(origin.Y);

        if (!world.InBounds(cellX, cellY))
        {
            return new RaycastHit(TileId.Solid, 0f, hit: true);
        }

        if (world.IsBlocked(cellX, cellY))
        {
            return new RaycastHit(world.GetTile(cellX, cellY).Id, 0f, hit: true);
        }

        int stepX = direction.X >= 0f ? 1 : -1;
        int stepY = direction.Y >= 0f ? 1 : -1;

        float tDeltaX = direction.X == 0f ? float.PositiveInfinity : MathF.Abs(1f / direction.X);
        float tDeltaY = direction.Y == 0f ? float.PositiveInfinity : MathF.Abs(1f / direction.Y);

        float tMaxX = direction.X == 0f
            ? float.PositiveInfinity
            : ((stepX > 0 ? (cellX + 1f - origin.X) : (origin.X - cellX)) * tDeltaX);
        float tMaxY = direction.Y == 0f
            ? float.PositiveInfinity
            : ((stepY > 0 ? (cellY + 1f - origin.Y) : (origin.Y - cellY)) * tDeltaY);

        float traveled = 0f;

        while (traveled <= maxDistance)
        {
            if (tMaxX < tMaxY)
            {
                cellX += stepX;
                traveled = tMaxX;
                tMaxX += tDeltaX;
            }
            else if (tMaxY < tMaxX)
            {
                cellY += stepY;
                traveled = tMaxY;
                tMaxY += tDeltaY;
            }
            else
            {
                cellX += stepX;
                cellY += stepY;
                traveled = tMaxX;
                tMaxX += tDeltaX;
                tMaxY += tDeltaY;
            }

            if (traveled > maxDistance)
            {
                break;
            }

            if (!world.InBounds(cellX, cellY))
            {
                return new RaycastHit(TileId.Solid, traveled, hit: true);
            }

            if (world.IsBlocked(cellX, cellY))
            {
                return new RaycastHit(world.GetTile(cellX, cellY).Id, traveled, hit: true);
            }
        }

        return new RaycastHit(TileId.Empty, maxDistance, hit: false);
    }

    internal readonly struct RaycastHit
    {
        public RaycastHit(TileId tileId, float distance, bool hit)
        {
            TileId = tileId;
            Distance = distance;
            Hit = hit;
        }

        public TileId TileId { get; }

        public float Distance { get; }

        public bool Hit { get; }
    }

    private static float Quantize(float value)
    {
        return MathF.Round(value * QuantizeScale) / QuantizeScale;
    }
}
