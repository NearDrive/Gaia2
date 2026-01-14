using System.Numerics;

namespace Core.Sim;

public sealed class AgentState
{
    public AgentState(Vector2 position)
    {
        Position = position;
        LastVision = Array.Empty<float>();
    }

    public Vector2 Position { get; private set; }

    public float[] LastVision { get; private set; }

    public bool TryApplyDelta(GridWorld world, Vector2 delta)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        Vector2 next = Position + delta;
        if (world.IsBlocked(next))
        {
            return false;
        }

        Position = next;
        return true;
    }

    public void UpdateVision(float[] vision)
    {
        LastVision = vision ?? throw new ArgumentNullException(nameof(vision));
    }
}
