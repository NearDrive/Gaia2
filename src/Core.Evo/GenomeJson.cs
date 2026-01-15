using System.Text;
using System.Text.Json;
using System.Linq;

namespace Core.Evo;

public static class GenomeJson
{
    public static string Serialize(Genome genome)
    {
        if (genome is null)
        {
            throw new ArgumentNullException(nameof(genome));
        }

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        WriteGenome(writer, genome);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static Genome Deserialize(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return ReadGenome(document.RootElement);
    }

    internal static void WriteGenome(Utf8JsonWriter writer, Genome genome)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("nodes");
        writer.WriteStartArray();
        foreach (NodeGene node in genome.Nodes.OrderBy(node => node.Id))
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", node.Id);
            writer.WriteString("type", node.Type.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WritePropertyName("connections");
        writer.WriteStartArray();
        foreach (ConnectionGene connection in genome.Connections.OrderBy(connection => connection.InnovationId)
                     .ThenBy(connection => connection.InNodeId)
                     .ThenBy(connection => connection.OutNodeId))
        {
            writer.WriteStartObject();
            writer.WriteNumber("inNodeId", connection.InNodeId);
            writer.WriteNumber("outNodeId", connection.OutNodeId);
            writer.WriteNumber("weight", connection.Weight);
            writer.WriteBoolean("enabled", connection.Enabled);
            writer.WriteNumber("innovationId", connection.InnovationId);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    internal static Genome ReadGenome(JsonElement element)
    {
        List<NodeGene> nodes = new();
        foreach (JsonElement nodeElement in element.GetProperty("nodes").EnumerateArray())
        {
            int id = nodeElement.GetProperty("id").GetInt32();
            string typeValue = nodeElement.GetProperty("type").GetString() ?? string.Empty;
            NodeType type = Enum.Parse<NodeType>(typeValue, ignoreCase: true);
            nodes.Add(new NodeGene(id, type));
        }

        List<ConnectionGene> connections = new();
        foreach (JsonElement connectionElement in element.GetProperty("connections").EnumerateArray())
        {
            int inNodeId = connectionElement.GetProperty("inNodeId").GetInt32();
            int outNodeId = connectionElement.GetProperty("outNodeId").GetInt32();
            double weight = connectionElement.GetProperty("weight").GetDouble();
            bool enabled = connectionElement.GetProperty("enabled").GetBoolean();
            int innovationId = connectionElement.GetProperty("innovationId").GetInt32();

            connections.Add(new ConnectionGene(inNodeId, outNodeId, weight, enabled, innovationId));
        }

        nodes.Sort((a, b) => a.Id.CompareTo(b.Id));
        connections.Sort((a, b) =>
        {
            int compare = a.InnovationId.CompareTo(b.InnovationId);
            if (compare != 0)
            {
                return compare;
            }

            compare = a.InNodeId.CompareTo(b.InNodeId);
            if (compare != 0)
            {
                return compare;
            }

            return a.OutNodeId.CompareTo(b.OutNodeId);
        });

        return new Genome(nodes, connections);
    }
}
