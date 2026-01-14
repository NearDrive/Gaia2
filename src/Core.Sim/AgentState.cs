using System.Numerics;

namespace Core.Sim;

public sealed class AgentState
{
    public AgentState(Vector2 position)
    {
        Position = position;
    }

    public Vector2 Position { get; private set; }

    public void ApplyDelta(Vector2 delta)
    {
        Position += delta;
    }
}
