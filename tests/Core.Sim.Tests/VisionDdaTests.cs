using System.Numerics;
using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class VisionDdaTests
{
    [Fact]
    public void RaycastDda_HitsExpectedTiles()
    {
        GridWorld world = CreateTestWorld();
        Vector2 origin = new(1.5f, 1.5f);

        VisionSensor.RaycastHit rightHit = VisionSensor.RaycastDda(world, origin, Vector2.UnitX, maxDistance: 10f);
        Assert.True(rightHit.Hit);
        Assert.Equal(TileId.Solid, rightHit.TileId);
        Assert.InRange(rightHit.Distance, 2.49f, 2.51f);

        VisionSensor.RaycastHit upHit = VisionSensor.RaycastDda(world, origin, Vector2.UnitY, maxDistance: 10f);
        Assert.True(upHit.Hit);
        Assert.Equal(TileId.Water, upHit.TileId);
        Assert.InRange(upHit.Distance, 2.49f, 2.51f);

        Vector2 diagonal = Vector2.Normalize(new Vector2(1f, 1f));
        VisionSensor.RaycastHit diagHit = VisionSensor.RaycastDda(world, origin, diagonal, maxDistance: 10f);
        Assert.True(diagHit.Hit);
        Assert.Equal(TileId.Solid, diagHit.TileId);
        Assert.InRange(diagHit.Distance, 2.11f, 2.13f);
    }

    [Fact]
    public void RaycastDda_NoHitWithinRange_ReturnsEmpty()
    {
        GridWorld world = CreateTestWorld();
        Vector2 origin = new(1.5f, 1.5f);

        VisionSensor.RaycastHit hit = VisionSensor.RaycastDda(world, origin, Vector2.UnitX, maxDistance: 1f);

        Assert.False(hit.Hit);
        Assert.Equal(TileId.Empty, hit.TileId);
        Assert.Equal(1f, hit.Distance);
    }

    private static GridWorld CreateTestWorld()
    {
        GridWorld world = new(6, 6, seed: 1);

        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                world.SetTile(x, y, TileId.Empty);
            }
        }

        world.SetTile(4, 1, TileId.Solid);
        world.SetTile(1, 4, TileId.Water);
        world.SetTile(3, 3, TileId.Solid);

        return world;
    }
}
