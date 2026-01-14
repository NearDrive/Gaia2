using System.Numerics;

namespace Core.Sim;

public sealed class VisionSensor
{
    private const float StepSize = 0.25f;
    private const float QuantizeScale = 1000f;

    public VisionSensor(int rayCount, float maxDistance, float fovRadians)
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

        RayCount = rayCount;
        MaxDistance = maxDistance;
        FovRadians = fovRadians;
    }

    public int RayCount { get; }

    public float MaxDistance { get; }

    public float FovRadians { get; }

    public float[] Sense(GridWorld world, Vector2 agentPos, float agentFacingRadians)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        float[] results = new float[RayCount * 3];
        float startAngle = agentFacingRadians - (FovRadians * 0.5f);
        float angleStep = RayCount > 1 ? FovRadians / (RayCount - 1) : 0f;

        for (int i = 0; i < RayCount; i += 1)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            float distanceNorm = 1f;
            float embedding = 0f;
            float affordance = 0f;

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
                    embedding = HashToUnitFloat(hitId);
                    affordance = hitId == TileId.Water ? 1f : 0f;
                    break;
                }
            }

            int offset = i * 3;
            results[offset] = distanceNorm;
            results[offset + 1] = embedding;
            results[offset + 2] = affordance;
        }

        return results;
    }

    private static float Quantize(float value)
    {
        return MathF.Round(value * QuantizeScale) / QuantizeScale;
    }

    private static float HashToUnitFloat(TileId id)
    {
        ulong state = (ulong)id + 0x9E3779B97F4A7C15UL;
        state = SplitMix64(state);
        double scaled = (state >> 11) * (1.0 / (1UL << 53));
        return (float)scaled;
    }

    private static ulong SplitMix64(ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
