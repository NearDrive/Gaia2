namespace Core.Evo;

public readonly record struct BrainInput(ReadOnlyMemory<float> Vision, float Thirst01, float Bias = 1f);
