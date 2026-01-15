namespace Core.Evo;

public enum NodeType
{
    Input,
    Hidden,
    Output
}

public readonly record struct NodeGene(int Id, NodeType Type);

public readonly record struct ConnectionGene(
    int InNodeId,
    int OutNodeId,
    double Weight,
    bool Enabled,
    int InnovationId);

public sealed class Genome
{
    private readonly List<NodeGene> _nodes;
    private readonly List<ConnectionGene> _connections;

    public Genome(IEnumerable<NodeGene> nodes, IEnumerable<ConnectionGene> connections)
    {
        if (nodes is null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        if (connections is null)
        {
            throw new ArgumentNullException(nameof(connections));
        }

        _nodes = new List<NodeGene>(nodes);
        _connections = new List<ConnectionGene>(connections);
    }

    public IReadOnlyList<NodeGene> Nodes => _nodes;

    public IReadOnlyList<ConnectionGene> Connections => _connections;

    public int NodeCount => _nodes.Count;

    public int ConnectionCount => _connections.Count;

    public Genome Clone()
    {
        return new Genome(_nodes, _connections);
    }

    internal List<NodeGene> MutableNodes => _nodes;

    internal List<ConnectionGene> MutableConnections => _connections;
}
