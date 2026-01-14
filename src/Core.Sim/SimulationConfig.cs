namespace Core.Sim;

public readonly record struct SimulationConfig(
    int Seed,
    float Dt,
    int TicksPerEpisode,
    int WorldWidth,
    int WorldHeight,
    int AgentVisionRays,
    float AgentVisionRange,
    float AgentFov);
