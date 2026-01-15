namespace Core.Sim;

public sealed class CurriculumSchedule
{
    private readonly CurriculumPhase _start;
    private readonly CurriculumPhase _end;

    public CurriculumSchedule(int totalGenerations, CurriculumPhase start, CurriculumPhase end)
    {
        if (totalGenerations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalGenerations));
        }

        TotalGenerations = totalGenerations;
        _start = start;
        _end = end;
    }

    public int TotalGenerations { get; }

    public CurriculumPhase GetPhase(int generation)
    {
        if (generation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generation));
        }

        if (TotalGenerations == 1)
        {
            return _end;
        }

        float t = Math.Clamp(generation / (float)(TotalGenerations - 1), 0f, 1f);
        return CurriculumPhase.Lerp(_start, _end, t);
    }
}

public readonly record struct CurriculumPhase(
    float WaterProximityBias01,
    float ObstacleDensity01,
    int WorldSize,
    int TicksPerEpisode,
    float ThirstRatePerSecond)
{
    public static CurriculumPhase Lerp(CurriculumPhase start, CurriculumPhase end, float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        int worldSize = (int)Math.Round(Lerp(start.WorldSize, end.WorldSize, t), MidpointRounding.AwayFromZero);
        int ticksPerEpisode = (int)Math.Round(
            Lerp(start.TicksPerEpisode, end.TicksPerEpisode, t),
            MidpointRounding.AwayFromZero);

        return new CurriculumPhase(
            WaterProximityBias01: Lerp(start.WaterProximityBias01, end.WaterProximityBias01, t),
            ObstacleDensity01: Lerp(start.ObstacleDensity01, end.ObstacleDensity01, t),
            WorldSize: Math.Max(1, worldSize),
            TicksPerEpisode: Math.Max(1, ticksPerEpisode),
            ThirstRatePerSecond: Lerp(start.ThirstRatePerSecond, end.ThirstRatePerSecond, t));
    }

    private static float Lerp(float start, float end, float t)
    {
        return start + ((end - start) * t);
    }
}
