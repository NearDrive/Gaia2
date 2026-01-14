using System.Numerics;

namespace Core.Sim;

public static class SimulationChecksum
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Compute(IReadOnlyList<AgentState> agents, int tick)
    {
        ulong hash = OffsetBasis;

        Add(ref hash, tick);
        Add(ref hash, agents.Count);

        foreach (AgentState agent in agents)
        {
            AddPosition(ref hash, agent.Position);
        }

        return hash;
    }

    private static void AddPosition(ref ulong hash, Vector2 position)
    {
        int x = (int)MathF.Round(position.X * 1000f);
        int y = (int)MathF.Round(position.Y * 1000f);
        Add(ref hash, x);
        Add(ref hash, y);
    }

    private static void Add(ref ulong hash, int value)
    {
        unchecked
        {
            hash ^= (uint)value;
            hash *= Prime;
        }
    }
}
