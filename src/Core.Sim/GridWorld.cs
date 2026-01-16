using System.Numerics;

namespace Core.Sim;

public sealed class GridWorld
{
    public const float DefaultObstacleDensity01 = 0.05f;
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;
    private readonly Tile[] _tiles;

    public GridWorld(int width, int height, int seed)
        : this(width, height, seed, DefaultObstacleDensity01, 0f, null)
    {
    }

    public GridWorld(int width, int height, int seed, float obstacleDensity01, float waterProximityBias01, Vector2? spawnHint)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        _tiles = new Tile[width * height];

        Generate(seed, obstacleDensity01, waterProximityBias01, spawnHint);
    }

    public int Width { get; }

    public int Height { get; }

    public Tile GetTile(int x, int y)
    {
        if (!InBounds(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        return _tiles[(y * Width) + x];
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsBlocked(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return true;
        }

        TileId id = _tiles[(y * Width) + x].Id;
        return id == TileId.Solid || id == TileId.Water;
    }

    public bool IsBlocked(Vector2 position)
    {
        int x = (int)MathF.Floor(position.X);
        int y = (int)MathF.Floor(position.Y);
        return IsBlocked(x, y);
    }

    public ulong ComputeChecksum()
    {
        ulong hash = OffsetBasis;
        Add(ref hash, Width);
        Add(ref hash, Height);

        for (int y = 0; y < Height; y += 1)
        {
            for (int x = 0; x < Width; x += 1)
            {
                Add(ref hash, (byte)_tiles[(y * Width) + x].Id);
            }
        }

        return hash;
    }

    private void Generate(int seed, float obstacleDensity01, float waterProximityBias01, Vector2? spawnHint)
    {
        for (int i = 0; i < _tiles.Length; i += 1)
        {
            _tiles[i] = new Tile(TileId.Empty);
        }

        SimRng rng = new SimRng(seed);
        float clampedDensity = Math.Clamp(obstacleDensity01, 0f, 1f);
        float clampedBias = Math.Clamp(waterProximityBias01, 0f, 1f);
        int minDim = Math.Min(Width, Height);
        float radius = Math.Max(1f, (minDim * 0.1f) + (minDim * 0.1f * rng.NextFloat()));
        Vector2 center = new(
            rng.NextFloat(0f, Width),
            rng.NextFloat(0f, Height));

        if (spawnHint.HasValue && clampedBias > 0f)
        {
            Vector2 spawn = spawnHint.Value;
            spawn = new Vector2(
                Math.Clamp(spawn.X, 0f, Math.Max(0f, Width - 0.01f)),
                Math.Clamp(spawn.Y, 0f, Math.Max(0f, Height - 0.01f)));
            center = Vector2.Lerp(center, spawn, clampedBias);
        }

        float radiusSq = radius * radius;

        for (int y = 0; y < Height; y += 1)
        {
            for (int x = 0; x < Width; x += 1)
            {
                Vector2 delta = new Vector2(x + 0.5f - center.X, y + 0.5f - center.Y);
                if (delta.LengthSquared() <= radiusSq)
                {
                    SetTile(x, y, TileId.Water);
                }
            }
        }

        int targetSolids = (int)Math.Round(Width * Height * clampedDensity, MidpointRounding.AwayFromZero);
        targetSolids = Math.Max(1, targetSolids);
        int placed = 0;
        int attempts = 0;
        int maxAttempts = targetSolids * 10;

        while (placed < targetSolids && attempts < maxAttempts)
        {
            int x = (int)MathF.Floor(rng.NextFloat(0f, Width));
            int y = (int)MathF.Floor(rng.NextFloat(0f, Height));
            attempts += 1;

            int index = (y * Width) + x;
            if (_tiles[index].Id == TileId.Empty)
            {
                _tiles[index] = new Tile(TileId.Solid);
                placed += 1;
            }
        }

        if (placed == 0)
        {
            SetTile(0, 0, TileId.Solid);
        }
    }

    internal void SetTile(int x, int y, TileId id)
    {
        if (!InBounds(x, y))
        {
            return;
        }

        _tiles[(y * Width) + x] = new Tile(id);
    }

    private static void Add(ref ulong hash, int value)
    {
        unchecked
        {
            hash ^= (uint)value;
            hash *= Prime;
        }
    }

    private static void Add(ref ulong hash, byte value)
    {
        unchecked
        {
            hash ^= value;
            hash *= Prime;
        }
    }
}
