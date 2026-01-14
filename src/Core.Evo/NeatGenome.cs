using System.Text.Json;

namespace Core.Evo;

public sealed class NeatGenome
{
    public int InputCount { get; init; }

    public float[] Weights { get; init; } = Array.Empty<float>();

    public float BiasWeight { get; init; }

    public static NeatGenome CreateDeterministic(int inputCount)
    {
        if (inputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputCount));
        }

        float[] weights = new float[inputCount];
        for (int i = 0; i < weights.Length; i += 1)
        {
            weights[i] = HashToWeight(i + 1);
        }

        return new NeatGenome
        {
            InputCount = inputCount,
            Weights = weights,
            BiasWeight = HashToWeight(99991)
        };
    }

    public static NeatGenome Load(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string json = File.ReadAllText(path);
        NeatGenome? genome = JsonSerializer.Deserialize<NeatGenome>(json);
        if (genome is null)
        {
            throw new InvalidOperationException("Failed to deserialize genome.");
        }

        return genome;
    }

    public void Save(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        JsonSerializerOptions options = new() { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }

    private static float HashToWeight(int value)
    {
        uint state = (uint)value * 0x9E3779B9u;
        state ^= state >> 16;
        state *= 0x7FEB352Du;
        state ^= state >> 15;
        state *= 0x846CA68Bu;
        state ^= state >> 16;
        float unit = state / (float)uint.MaxValue;
        return (unit * 2f) - 1f;
    }
}
