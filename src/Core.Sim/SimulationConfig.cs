using System;

namespace Core.Sim;

public readonly record struct SimulationConfig
{
    public SimulationConfig(
        int seed,
        float dt,
        int ticksPerEpisode,
        int worldWidth,
        int worldHeight,
        int agentVisionRays,
        float agentVisionRange,
        float agentFov,
        float agentMaxSpeed = 1.5f,
        float agentTurnRateRad = MathF.PI / 4f,
        float moveDeadzone = 0.05f,
        float thirstRatePerSecond = 0.01f,
        float deathGraceSeconds = 3f,
        float waterProximityBias01 = 0f,
        float obstacleDensity01 = 0.05f,
        int embeddingDimension = 8,
        int embeddingSeed = 0,
        int actionPreferenceCount = 4)
    {
        Seed = seed;
        Dt = dt;
        TicksPerEpisode = ticksPerEpisode;
        WorldWidth = worldWidth;
        WorldHeight = worldHeight;
        AgentVisionRays = agentVisionRays;
        AgentVisionRange = agentVisionRange;
        AgentFov = agentFov;
        AgentMaxSpeed = agentMaxSpeed;
        AgentTurnRateRad = agentTurnRateRad;
        MoveDeadzone = moveDeadzone;
        ThirstRatePerSecond = thirstRatePerSecond;
        DeathGraceSeconds = deathGraceSeconds;
        WaterProximityBias01 = waterProximityBias01;
        ObstacleDensity01 = obstacleDensity01;
        EmbeddingDimension = embeddingDimension;
        EmbeddingSeed = embeddingSeed;
        ActionPreferenceCount = actionPreferenceCount;
    }

    public int Seed { get; init; }
    public float Dt { get; init; }
    public int TicksPerEpisode { get; init; }
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public int AgentVisionRays { get; init; }
    public float AgentVisionRange { get; init; }
    public float AgentFov { get; init; }
    public float AgentMaxSpeed { get; init; }
    public float AgentTurnRateRad { get; init; }
    public float MoveDeadzone { get; init; }
    public float ThirstRatePerSecond { get; init; }
    public float DeathGraceSeconds { get; init; }
    public float WaterProximityBias01 { get; init; }
    public float ObstacleDensity01 { get; init; }
    public int EmbeddingDimension { get; init; }
    public int EmbeddingSeed { get; init; }
    public int ActionPreferenceCount { get; init; }
}
