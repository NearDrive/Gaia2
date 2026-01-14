namespace Core.Sim;

public readonly struct Tile
{
    public Tile(TileId id)
    {
        Id = id;
    }

    public TileId Id { get; }
}
