namespace Core.Evo;

public sealed class PopulationSnapshot
{
    public PopulationSnapshot(Population population, EvolutionConfig config, RngState rngState)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        RngState = rngState;
    }

    public Population Population { get; }

    public EvolutionConfig Config { get; }

    public RngState RngState { get; }
}
