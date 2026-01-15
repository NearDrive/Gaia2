namespace Core.Sim;

public readonly record struct SimulationConfig(
    int Seed,
    float Dt,
    int TicksPerEpisode,
    int WorldWidth,
    int WorldHeight,
    int AgentVisionRays,
    float AgentVisionRange,
    float AgentFov,
    float AgentMaxSpeed = 1.5f,
    float MoveDeadzone = 0.05f,
    float ThirstRatePerSecond = 0.01f,
    float DeathGraceSeconds = 3f);
