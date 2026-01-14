namespace Core.Sim;

public readonly record struct SimulationConfig(int Seed, float Dt, int TicksPerEpisode);
