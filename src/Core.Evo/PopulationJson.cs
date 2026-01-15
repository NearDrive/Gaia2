using System.Text;
using System.Text.Json;

namespace Core.Evo;

public static class PopulationJson
{
    public static string Serialize(Population population, EvolutionConfig config, RngState rngState)
    {
        if (population is null)
        {
            throw new ArgumentNullException(nameof(population));
        }

        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        writer.WritePropertyName("population");
        writer.WriteStartObject();
        writer.WriteNumber("generation", population.Generation);
        writer.WritePropertyName("genomes");
        writer.WriteStartArray();
        foreach (Genome genome in population.Genomes)
        {
            GenomeJson.WriteGenome(writer, genome);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.WritePropertyName("config");
        writer.WriteStartObject();
        writer.WriteNumber("populationSize", config.PopulationSize);
        writer.WriteNumber("eliteCount", config.EliteCount);
        writer.WriteNumber("mutationRate", config.MutationRate);
        writer.WriteNumber("addConnectionRate", config.AddConnectionRate);
        writer.WriteNumber("addNodeRate", config.AddNodeRate);
        writer.WriteNumber("weightPerturbRate", config.WeightPerturbRate);
        writer.WriteNumber("weightResetRate", config.WeightResetRate);
        writer.WriteNumber("maxNodes", config.MaxNodes);
        writer.WriteNumber("maxConnections", config.MaxConnections);
        writer.WriteEndObject();

        writer.WritePropertyName("rngState");
        writer.WriteStartObject();
        writer.WriteNumber("state", rngState.State);
        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static PopulationSnapshot Deserialize(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        JsonElement populationElement = root.GetProperty("population");
        int generation = populationElement.GetProperty("generation").GetInt32();

        List<Genome> genomes = new();
        foreach (JsonElement genomeElement in populationElement.GetProperty("genomes").EnumerateArray())
        {
            genomes.Add(GenomeJson.ReadGenome(genomeElement));
        }

        Population population = new(genomes, generation);

        JsonElement configElement = root.GetProperty("config");
        EvolutionConfig config = new(
            configElement.GetProperty("populationSize").GetInt32(),
            configElement.GetProperty("eliteCount").GetInt32(),
            configElement.GetProperty("mutationRate").GetDouble(),
            configElement.GetProperty("addConnectionRate").GetDouble(),
            configElement.GetProperty("addNodeRate").GetDouble(),
            configElement.GetProperty("weightPerturbRate").GetDouble(),
            configElement.GetProperty("weightResetRate").GetDouble(),
            configElement.GetProperty("maxNodes").GetInt32(),
            configElement.GetProperty("maxConnections").GetInt32());

        JsonElement rngElement = root.GetProperty("rngState");
        ulong state = rngElement.GetProperty("state").GetUInt64();

        return new PopulationSnapshot(population, config, new RngState(state));
    }
}
