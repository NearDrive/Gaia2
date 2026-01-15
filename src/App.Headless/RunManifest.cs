using System.Text.Json;

namespace App.Headless;

internal sealed record EvolutionConfigManifest(
    int EliteCount,
    double MutationRate,
    double AddConnectionRate,
    double AddNodeRate,
    double WeightPerturbRate,
    double WeightResetRate,
    int MaxNodes,
    int MaxConnections);

internal sealed record RunManifestSummary(
    double BestFitness,
    double MeanFitness,
    int BestGenomeNodes,
    int BestGenomeConnections,
    int BestTicksSurvived,
    int BestSuccessfulDrinks,
    float BestAvgThirst01);

internal sealed record RunManifest(
    int SchemaVersion,
    string CreatedUtc,
    int Seed,
    int Generations,
    int Population,
    int EpisodesPerGenome,
    int TicksPerEpisode,
    int Parallel,
    int MaxDegree,
    EvolutionConfigManifest EvolutionConfig,
    string BestGenomePath,
    string CheckpointPath,
    string TrainLogPath,
    RunManifestSummary FinalSummary);

internal static class RunManifestJson
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static RunManifest Read(string path)
    {
        string json = File.ReadAllText(path);
        RunManifest? manifest = JsonSerializer.Deserialize<RunManifest>(json, ReadOptions);
        if (manifest is null)
        {
            throw new InvalidOperationException($"Unable to parse manifest at {path}.");
        }

        return manifest;
    }

    public static void Write(string path, RunManifest manifest)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream stream = File.Create(path);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", manifest.SchemaVersion);
        writer.WriteString("createdUtc", manifest.CreatedUtc);
        writer.WriteNumber("seed", manifest.Seed);
        writer.WriteNumber("generations", manifest.Generations);
        writer.WriteNumber("population", manifest.Population);
        writer.WriteNumber("episodesPerGenome", manifest.EpisodesPerGenome);
        writer.WriteNumber("ticksPerEpisode", manifest.TicksPerEpisode);
        writer.WriteNumber("parallel", manifest.Parallel);
        writer.WriteNumber("maxDegree", manifest.MaxDegree);
        writer.WritePropertyName("evolutionConfig");
        writer.WriteStartObject();
        writer.WriteNumber("eliteCount", manifest.EvolutionConfig.EliteCount);
        writer.WriteNumber("mutationRate", manifest.EvolutionConfig.MutationRate);
        writer.WriteNumber("addConnectionRate", manifest.EvolutionConfig.AddConnectionRate);
        writer.WriteNumber("addNodeRate", manifest.EvolutionConfig.AddNodeRate);
        writer.WriteNumber("weightPerturbRate", manifest.EvolutionConfig.WeightPerturbRate);
        writer.WriteNumber("weightResetRate", manifest.EvolutionConfig.WeightResetRate);
        writer.WriteNumber("maxNodes", manifest.EvolutionConfig.MaxNodes);
        writer.WriteNumber("maxConnections", manifest.EvolutionConfig.MaxConnections);
        writer.WriteEndObject();
        writer.WriteString("bestGenomePath", manifest.BestGenomePath);
        writer.WriteString("checkpointPath", manifest.CheckpointPath);
        writer.WriteString("trainLogPath", manifest.TrainLogPath);
        writer.WritePropertyName("finalSummary");
        writer.WriteStartObject();
        writer.WriteNumber("bestFitness", manifest.FinalSummary.BestFitness);
        writer.WriteNumber("meanFitness", manifest.FinalSummary.MeanFitness);
        writer.WriteNumber("bestGenomeNodes", manifest.FinalSummary.BestGenomeNodes);
        writer.WriteNumber("bestGenomeConnections", manifest.FinalSummary.BestGenomeConnections);
        writer.WriteNumber("bestTicksSurvived", manifest.FinalSummary.BestTicksSurvived);
        writer.WriteNumber("bestSuccessfulDrinks", manifest.FinalSummary.BestSuccessfulDrinks);
        writer.WriteNumber("bestAvgThirst01", manifest.FinalSummary.BestAvgThirst01);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
    }
}

internal static class RunManifestComparer
{
    public static IReadOnlyList<string> Compare(RunManifest a, RunManifest b)
    {
        List<string> differences = new();

        if (a.SchemaVersion != b.SchemaVersion)
        {
            differences.Add("schemaVersion");
        }

        if (a.Seed != b.Seed)
        {
            differences.Add("seed");
        }

        if (a.Generations != b.Generations)
        {
            differences.Add("generations");
        }

        if (a.Population != b.Population)
        {
            differences.Add("population");
        }

        if (a.EpisodesPerGenome != b.EpisodesPerGenome)
        {
            differences.Add("episodesPerGenome");
        }

        if (a.TicksPerEpisode != b.TicksPerEpisode)
        {
            differences.Add("ticksPerEpisode");
        }

        if (a.Parallel != b.Parallel)
        {
            differences.Add("parallel");
        }

        if (a.MaxDegree != b.MaxDegree)
        {
            differences.Add("maxDegree");
        }

        AppendEvolutionConfigDifferences(differences, a.EvolutionConfig, b.EvolutionConfig);
        AppendSummaryDifferences(differences, a.FinalSummary, b.FinalSummary);

        if (!string.Equals(a.BestGenomePath, b.BestGenomePath, StringComparison.Ordinal))
        {
            differences.Add("bestGenomePath");
        }

        if (!string.Equals(a.CheckpointPath, b.CheckpointPath, StringComparison.Ordinal))
        {
            differences.Add("checkpointPath");
        }

        if (!string.Equals(a.TrainLogPath, b.TrainLogPath, StringComparison.Ordinal))
        {
            differences.Add("trainLogPath");
        }

        return differences;
    }

    private static void AppendEvolutionConfigDifferences(
        ICollection<string> differences,
        EvolutionConfigManifest a,
        EvolutionConfigManifest b)
    {
        if (a.EliteCount != b.EliteCount)
        {
            differences.Add("evolutionConfig.eliteCount");
        }

        if (a.MutationRate != b.MutationRate)
        {
            differences.Add("evolutionConfig.mutationRate");
        }

        if (a.AddConnectionRate != b.AddConnectionRate)
        {
            differences.Add("evolutionConfig.addConnectionRate");
        }

        if (a.AddNodeRate != b.AddNodeRate)
        {
            differences.Add("evolutionConfig.addNodeRate");
        }

        if (a.WeightPerturbRate != b.WeightPerturbRate)
        {
            differences.Add("evolutionConfig.weightPerturbRate");
        }

        if (a.WeightResetRate != b.WeightResetRate)
        {
            differences.Add("evolutionConfig.weightResetRate");
        }

        if (a.MaxNodes != b.MaxNodes)
        {
            differences.Add("evolutionConfig.maxNodes");
        }

        if (a.MaxConnections != b.MaxConnections)
        {
            differences.Add("evolutionConfig.maxConnections");
        }
    }

    private static void AppendSummaryDifferences(
        ICollection<string> differences,
        RunManifestSummary a,
        RunManifestSummary b)
    {
        if (a.BestFitness != b.BestFitness)
        {
            differences.Add("finalSummary.bestFitness");
        }

        if (a.MeanFitness != b.MeanFitness)
        {
            differences.Add("finalSummary.meanFitness");
        }

        if (a.BestGenomeNodes != b.BestGenomeNodes)
        {
            differences.Add("finalSummary.bestGenomeNodes");
        }

        if (a.BestGenomeConnections != b.BestGenomeConnections)
        {
            differences.Add("finalSummary.bestGenomeConnections");
        }

        if (a.BestTicksSurvived != b.BestTicksSurvived)
        {
            differences.Add("finalSummary.bestTicksSurvived");
        }

        if (a.BestSuccessfulDrinks != b.BestSuccessfulDrinks)
        {
            differences.Add("finalSummary.bestSuccessfulDrinks");
        }

        if (a.BestAvgThirst01 != b.BestAvgThirst01)
        {
            differences.Add("finalSummary.bestAvgThirst01");
        }
    }
}
