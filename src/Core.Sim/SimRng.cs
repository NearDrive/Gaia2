namespace Core.Sim;

public sealed class SimRng
{
    private ulong _state;

    public SimRng(int seed)
    {
        _state = (ulong)seed;
    }

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public float NextFloat()
    {
        ulong value = NextUInt64();
        double scaled = (value >> 11) * (1.0 / (1UL << 53));
        return (float)scaled;
    }

    public float NextFloat(float minInclusive, float maxExclusive)
    {
        return minInclusive + ((maxExclusive - minInclusive) * NextFloat());
    }
}
