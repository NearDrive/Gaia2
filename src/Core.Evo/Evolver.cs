namespace Core.Evo;

public sealed class Evolver
{
    private const double ComplexityNodePenalty = 0.01;
    private const double ComplexityConnectionPenalty = 0.001;
    private const double SelectionFraction = 0.2;
    private readonly EvolutionConfig _config;
    private readonly Dictionary<(int InNodeId, int OutNodeId), int> _innovationIds = new();
    private int _nextInnovationId;
    private int _nextNodeId;

    public Evolver(EvolutionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Population CreateInitialPopulation(int seed, int populationSize, int inputCount, int outputCount)
    {
        if (populationSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(populationSize));
        }

        if (inputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputCount));
        }

        if (outputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputCount));
        }

        _innovationIds.Clear();
        _nextInnovationId = 0;
        _nextNodeId = inputCount + outputCount;

        EvoRng rng = new(seed);

        List<NodeGene> nodes = new(inputCount + outputCount);
        for (int i = 0; i < inputCount; i += 1)
        {
            nodes.Add(new NodeGene(i, NodeType.Input));
        }

        for (int i = 0; i < outputCount; i += 1)
        {
            nodes.Add(new NodeGene(inputCount + i, NodeType.Output));
        }

        List<ConnectionGene> connections = new();
        for (int input = 0; input < inputCount; input += 1)
        {
            for (int output = 0; output < outputCount; output += 1)
            {
                int outId = inputCount + output;
                int innovation = GetInnovationId(input, outId);
                double weight = rng.NextDouble(-1.0, 1.0);
                connections.Add(new ConnectionGene(input, outId, weight, true, innovation));
            }
        }

        connections.Sort((a, b) => a.InnovationId.CompareTo(b.InnovationId));
        Genome template = new(nodes, connections);

        List<Genome> genomes = new(populationSize);
        for (int i = 0; i < populationSize; i += 1)
        {
            genomes.Add(template.Clone());
        }

        return new Population(genomes, 0);
    }

    public Population NextGeneration(Population current, IReadOnlyList<double> fitnesses, int seed)
    {
        EvoRng rng = new(seed);
        return NextGeneration(current, fitnesses, rng);
    }

    public Population NextGeneration(Population current, IReadOnlyList<double> fitnesses, EvoRng rng)
    {
        if (current is null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        if (fitnesses is null)
        {
            throw new ArgumentNullException(nameof(fitnesses));
        }

        if (fitnesses.Count != current.Genomes.Count)
        {
            throw new ArgumentException("Fitness list must match genome count.", nameof(fitnesses));
        }

        List<(int Index, double Fitness)> scored = new(current.Genomes.Count);
        for (int i = 0; i < current.Genomes.Count; i += 1)
        {
            Genome genome = current.Genomes[i];
            double penalty = (ComplexityNodePenalty * genome.NodeCount)
                + (ComplexityConnectionPenalty * genome.ConnectionCount);
            scored.Add((i, fitnesses[i] - penalty));
        }

        scored.Sort((a, b) =>
        {
            int fitnessCompare = b.Fitness.CompareTo(a.Fitness);
            return fitnessCompare != 0 ? fitnessCompare : a.Index.CompareTo(b.Index);
        });

        int eliteCount = Math.Min(_config.EliteCount, scored.Count);
        int selectionCount = Math.Max(1, (int)Math.Ceiling(scored.Count * SelectionFraction));

        List<Genome> nextGen = new(scored.Count);
        for (int i = 0; i < eliteCount; i += 1)
        {
            nextGen.Add(current.Genomes[scored[i].Index].Clone());
        }

        for (int i = eliteCount; i < scored.Count; i += 1)
        {
            int parentIndex = scored[rng.NextInt(0, selectionCount)].Index;
            Genome child = current.Genomes[parentIndex].Clone();

            if (rng.NextDouble() < _config.MutationRate)
            {
                Mutate(child, rng);
            }

            nextGen.Add(child);
        }

        return new Population(nextGen, current.Generation + 1);
    }

    public void RestoreStateFromPopulation(Population population)
    {
        if (population is null)
        {
            throw new ArgumentNullException(nameof(population));
        }

        _innovationIds.Clear();
        _nextInnovationId = 0;
        _nextNodeId = 0;

        foreach (Genome genome in population.Genomes)
        {
            foreach (NodeGene node in genome.Nodes)
            {
                _nextNodeId = Math.Max(_nextNodeId, node.Id + 1);
            }

            foreach (ConnectionGene connection in genome.Connections)
            {
                (int InNodeId, int OutNodeId) key = (connection.InNodeId, connection.OutNodeId);
                if (_innovationIds.TryGetValue(key, out int existing))
                {
                    if (existing != connection.InnovationId)
                    {
                        throw new InvalidOperationException(
                            $"Conflicting innovation ids for ({connection.InNodeId},{connection.OutNodeId}).");
                    }
                }
                else
                {
                    _innovationIds[key] = connection.InnovationId;
                }

                _nextInnovationId = Math.Max(_nextInnovationId, connection.InnovationId + 1);
            }
        }
    }

    private void Mutate(Genome genome, EvoRng rng)
    {
        if (rng.NextDouble() < _config.AddConnectionRate)
        {
            TryAddConnection(genome, rng);
        }

        if (rng.NextDouble() < _config.AddNodeRate)
        {
            TryAddNode(genome, rng);
        }

        List<ConnectionGene> connections = genome.MutableConnections;
        for (int i = 0; i < connections.Count; i += 1)
        {
            ConnectionGene connection = connections[i];
            if (!connection.Enabled)
            {
                continue;
            }

            double roll = rng.NextDouble();
            if (roll < _config.WeightResetRate)
            {
                double weight = rng.NextDouble(-1.0, 1.0);
                connections[i] = connection with { Weight = weight };
            }
            else if (roll < _config.WeightResetRate + _config.WeightPerturbRate)
            {
                double delta = rng.NextDouble(-0.5, 0.5);
                connections[i] = connection with { Weight = connection.Weight + delta };
            }
        }
    }

    private void TryAddConnection(Genome genome, EvoRng rng)
    {
        if (genome.ConnectionCount >= _config.MaxConnections)
        {
            return;
        }

        List<NodeGene> nodes = genome.MutableNodes;
        List<ConnectionGene> connections = genome.MutableConnections;
        HashSet<(int InNodeId, int OutNodeId)> existing = new();

        foreach (ConnectionGene connection in connections)
        {
            existing.Add((connection.InNodeId, connection.OutNodeId));
        }

        List<(int InId, int OutId)> candidates = new();

        foreach (NodeGene from in nodes)
        {
            if (from.Type == NodeType.Output)
            {
                continue;
            }

            foreach (NodeGene to in nodes)
            {
                if (to.Type == NodeType.Input)
                {
                    continue;
                }

                if (from.Id == to.Id)
                {
                    continue;
                }

                if (existing.Contains((from.Id, to.Id)))
                {
                    continue;
                }

                if (CreatesCycle(connections, from.Id, to.Id))
                {
                    continue;
                }

                candidates.Add((from.Id, to.Id));
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        (int inId, int outId) = candidates[rng.NextInt(0, candidates.Count)];
        int innovation = GetInnovationId(inId, outId);
        double weight = rng.NextDouble(-1.0, 1.0);
        connections.Add(new ConnectionGene(inId, outId, weight, true, innovation));
        connections.Sort((a, b) => a.InnovationId.CompareTo(b.InnovationId));
    }

    private void TryAddNode(Genome genome, EvoRng rng)
    {
        if (genome.NodeCount >= _config.MaxNodes)
        {
            return;
        }

        if (genome.ConnectionCount + 2 > _config.MaxConnections)
        {
            return;
        }

        List<ConnectionGene> connections = genome.MutableConnections;
        List<int> enabledIndexes = new();

        for (int i = 0; i < connections.Count; i += 1)
        {
            if (connections[i].Enabled)
            {
                enabledIndexes.Add(i);
            }
        }

        if (enabledIndexes.Count == 0)
        {
            return;
        }

        int index = enabledIndexes[rng.NextInt(0, enabledIndexes.Count)];
        ConnectionGene target = connections[index];
        connections[index] = target with { Enabled = false };

        int newNodeId = _nextNodeId;
        _nextNodeId += 1;

        List<NodeGene> nodes = genome.MutableNodes;
        nodes.Add(new NodeGene(newNodeId, NodeType.Hidden));
        nodes.Sort((a, b) => a.Id.CompareTo(b.Id));

        int inInnovation = GetInnovationId(target.InNodeId, newNodeId);
        int outInnovation = GetInnovationId(newNodeId, target.OutNodeId);

        connections.Add(new ConnectionGene(target.InNodeId, newNodeId, 1.0, true, inInnovation));
        connections.Add(new ConnectionGene(newNodeId, target.OutNodeId, target.Weight, true, outInnovation));
        connections.Sort((a, b) => a.InnovationId.CompareTo(b.InnovationId));
    }

    private int GetInnovationId(int inNodeId, int outNodeId)
    {
        (int, int) key = (inNodeId, outNodeId);
        if (_innovationIds.TryGetValue(key, out int existing))
        {
            return existing;
        }

        int id = _nextInnovationId;
        _nextInnovationId += 1;
        _innovationIds[key] = id;
        return id;
    }

    private static bool CreatesCycle(IReadOnlyList<ConnectionGene> connections, int fromId, int toId)
    {
        Dictionary<int, List<int>> adjacency = new();
        foreach (ConnectionGene connection in connections)
        {
            if (!connection.Enabled)
            {
                continue;
            }

            if (!adjacency.TryGetValue(connection.InNodeId, out List<int>? list))
            {
                list = new List<int>();
                adjacency[connection.InNodeId] = list;
            }

            list.Add(connection.OutNodeId);
        }

        Stack<int> stack = new();
        HashSet<int> visited = new();
        stack.Push(toId);

        while (stack.Count > 0)
        {
            int node = stack.Pop();
            if (!visited.Add(node))
            {
                continue;
            }

            if (node == fromId)
            {
                return true;
            }

            if (adjacency.TryGetValue(node, out List<int>? neighbors))
            {
                foreach (int next in neighbors)
                {
                    stack.Push(next);
                }
            }
        }

        return false;
    }
}
