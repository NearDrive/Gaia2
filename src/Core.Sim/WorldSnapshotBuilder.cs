using System;
using System.Collections.Generic;
using System.Numerics;

namespace Core.Sim;

public static class WorldSnapshotBuilder
{
    public static WorldSnapshot Build(Simulation simulation, int tick)
    {
        if (simulation is null)
        {
            throw new ArgumentNullException(nameof(simulation));
        }

        IReadOnlyList<AgentState> agents = simulation.Agents;
        AgentSnapshot[] agentSnapshots = new AgentSnapshot[agents.Count];

        for (int i = 0; i < agents.Count; i += 1)
        {
            AgentState agent = agents[i];
            Vector2 direction = agent.Direction;
            agentSnapshots[i] = new AgentSnapshot
            {
                Id = i,
                X = agent.Position.X,
                Y = agent.Position.Y,
                DirX = direction.X,
                DirY = direction.Y,
                Thirst01 = agent.Thirst01,
                Alive = agent.IsAlive
            };
        }

        ulong worldChecksum = simulation.World.ComputeChecksum();
        ulong agentsChecksum = SimulationChecksum.Compute(agents, simulation.Tick);
        ulong totalChecksum = SimulationChecksum.Combine(worldChecksum, agentsChecksum);

        SnapshotHeader header = new()
        {
            Tick = tick,
            WorldWidth = simulation.Config.WorldWidth,
            WorldHeight = simulation.Config.WorldHeight,
            AgentCount = agents.Count,
            WorldChecksum = totalChecksum
        };

        return new WorldSnapshot
        {
            Header = header,
            Agents = agentSnapshots
        };
    }
}
