namespace Core.Evo;

public sealed class Population
{
    public Population(IReadOnlyList<Genome> genomes, int generation)
    {
        Genomes = genomes ?? throw new ArgumentNullException(nameof(genomes));
        Generation = generation;
    }

    public IReadOnlyList<Genome> Genomes { get; }

    public int Generation { get; }
}
