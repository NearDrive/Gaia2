using System.Numerics;

namespace Core.Sim;

public sealed class GridWorld
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;
    private readonly Tile[] _tiles;

    public GridWorld(int width, int height, int seed)
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

        Generate(seed);
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

    private void Generate(int seed)
    {
        for (int i = 0; i < _tiles.Length; i += 1)
        {
            _tiles[i] = new Tile(TileId.Empty);
        }

        SimRng rng = new SimRng(seed);
        int minDim = Math.Min(Width, Height);
        float radius = Math.Max(1f, (minDim * 0.1f) + (minDim * 0.1f * rng.NextFloat()));
        Vector2 center = new Vector2(
            rng.NextFloat(0f, Width),
            rng.NextFloat(0f, Height));
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

        int targetSolids = Math.Max(1, (Width * Height) / 20);
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

    private void SetTile(int x, int y, TileId id)
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
