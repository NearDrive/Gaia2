using Core.Evo;

namespace Core.Sim;

public sealed class GenomeBrainFactory
{
    public IBrain CreateBrain(Genome genome, int inputCount, int outputCount)
    {
        if (genome is null)
        {
            throw new ArgumentNullException(nameof(genome));
        }

        if (inputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputCount));
        }

        if (outputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputCount));
        }

        return new GenomeBrain(genome, inputCount, outputCount);
    }

    private sealed class GenomeBrain : IBrain
    {
        private readonly IReadOnlyList<NodeGene> _nodes;
        private readonly IReadOnlyList<ConnectionGene> _connections;
        private readonly int _inputCount;
        private readonly int _outputCount;
        private readonly int[] _topologicalOrder;
        private readonly Dictionary<int, int> _nodeIndexById;
        private readonly List<ConnectionGene>[] _incoming;
        private readonly float[] _inputs;

        public GenomeBrain(Genome genome, int inputCount, int outputCount)
        {
            _nodes = genome.Nodes;
            _connections = genome.Connections;
            _inputCount = inputCount;
            _outputCount = outputCount;
            _inputs = new float[inputCount];

            _nodeIndexById = new Dictionary<int, int>(_nodes.Count);
            for (int i = 0; i < _nodes.Count; i += 1)
            {
                _nodeIndexById[_nodes[i].Id] = i;
            }

            _incoming = new List<ConnectionGene>[_nodes.Count];
            for (int i = 0; i < _nodes.Count; i += 1)
            {
                _incoming[i] = new List<ConnectionGene>();
            }

            foreach (ConnectionGene connection in _connections)
            {
                if (!connection.Enabled)
                {
                    continue;
                }

                if (_nodeIndexById.TryGetValue(connection.OutNodeId, out int targetIndex))
                {
                    _incoming[targetIndex].Add(connection);
                }
            }

            _topologicalOrder = BuildTopologicalOrder();
        }

        public AgentAction DecideAction(AgentState agent, Simulation simulation)
        {
            if (!agent.IsAlive)
            {
                return AgentAction.None;
            }

            Array.Clear(_inputs, 0, _inputs.Length);
            float[] vision = agent.LastVision;
            int visionCount = Math.Min(vision.Length, _inputCount - 1);
            if (visionCount > 0)
            {
                Array.Copy(vision, 0, _inputs, 0, visionCount);
            }

            _inputs[_inputCount - 1] = agent.Thirst01;

            float[] activations = new float[_nodes.Count];

            int inputIndex = 0;
            for (int i = 0; i < _nodes.Count && inputIndex < _inputCount; i += 1)
            {
                if (_nodes[i].Type == NodeType.Input)
                {
                    activations[i] = _inputs[inputIndex];
                    inputIndex += 1;
                }
            }

            foreach (int nodeIndex in _topologicalOrder)
            {
                NodeGene node = _nodes[nodeIndex];
                if (node.Type == NodeType.Input)
                {
                    continue;
                }

                double sum = 0.0;
                foreach (ConnectionGene connection in _incoming[nodeIndex])
                {
                    if (!_nodeIndexById.TryGetValue(connection.InNodeId, out int inIndex))
                    {
                        continue;
                    }

                    sum += activations[inIndex] * connection.Weight;
                }

                activations[nodeIndex] = (float)Math.Tanh(sum);
            }

            float outputValue = 0f;
            int outputSeen = 0;
            for (int i = 0; i < _nodes.Count; i += 1)
            {
                if (_nodes[i].Type != NodeType.Output)
                {
                    continue;
                }

                if (outputSeen == 0)
                {
                    outputValue = activations[i];
                }

                outputSeen += 1;
                if (outputSeen >= _outputCount)
                {
                    break;
                }
            }

            return outputValue > 0.5f ? AgentAction.Drink : AgentAction.None;
        }

        private int[] BuildTopologicalOrder()
        {
            int nodeCount = _nodes.Count;
            int[] indegree = new int[nodeCount];
            List<int>[] adjacency = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i += 1)
            {
                adjacency[i] = new List<int>();
            }

            foreach (ConnectionGene connection in _connections)
            {
                if (!connection.Enabled)
                {
                    continue;
                }

                if (!_nodeIndexById.TryGetValue(connection.InNodeId, out int fromIndex))
                {
                    continue;
                }

                if (!_nodeIndexById.TryGetValue(connection.OutNodeId, out int toIndex))
                {
                    continue;
                }

                adjacency[fromIndex].Add(toIndex);
                indegree[toIndex] += 1;
            }

            Queue<int> queue = new();
            for (int i = 0; i < nodeCount; i += 1)
            {
                if (indegree[i] == 0)
                {
                    queue.Enqueue(i);
                }
            }

            List<int> order = new(nodeCount);
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                order.Add(node);
                foreach (int next in adjacency[node])
                {
                    indegree[next] -= 1;
                    if (indegree[next] == 0)
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            if (order.Count != nodeCount)
            {
                order.Clear();
                for (int i = 0; i < nodeCount; i += 1)
                {
                    order.Add(i);
                }
            }

            return order.ToArray();
        }
    }
}
