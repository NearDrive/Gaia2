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
        private readonly float[] _activations;
        private readonly float[] _outputs;

        public GenomeBrain(Genome genome, int inputCount, int outputCount)
        {
            _nodes = genome.Nodes;
            _connections = genome.Connections;
            _inputCount = inputCount;
            _outputCount = outputCount;
            _inputs = new float[inputCount];
            _activations = new float[_nodes.Count];
            _outputs = new float[outputCount];

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

        public BrainOutput DecideAction(BrainInput input)
        {
            Array.Clear(_inputs, 0, _inputs.Length);
            float[] vision = input.Vision ?? Array.Empty<float>();
            int visionCount = Math.Min(vision.Length, _inputCount - 2);
            if (visionCount > 0)
            {
                Array.Copy(vision, 0, _inputs, 0, visionCount);
            }

            if (_inputCount >= 2)
            {
                _inputs[_inputCount - 2] = input.Thirst01;
                _inputs[_inputCount - 1] = input.Bias;
            }

            Array.Clear(_activations, 0, _activations.Length);

            int inputIndex = 0;
            for (int i = 0; i < _nodes.Count && inputIndex < _inputCount; i += 1)
            {
                if (_nodes[i].Type == NodeType.Input)
                {
                    _activations[i] = _inputs[inputIndex];
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

                    sum += _activations[inIndex] * connection.Weight;
                }

                _activations[nodeIndex] = (float)Math.Tanh(sum);
            }

            Array.Clear(_outputs, 0, _outputs.Length);
            int outputSeen = 0;
            for (int i = 0; i < _nodes.Count; i += 1)
            {
                if (_nodes[i].Type != NodeType.Output)
                {
                    continue;
                }

                if (outputSeen < _outputs.Length)
                {
                    _outputs[outputSeen] = _activations[i];
                }

                outputSeen += 1;
                if (outputSeen >= _outputCount)
                {
                    break;
                }
            }

            float rotationDelta = _outputCount > 0 ? Math.Clamp(_outputs[0], -1f, 1f) : 0f;
            float forwardSpeed = _outputCount > 1 ? Math.Clamp((_outputs[1] + 1f) * 0.5f, 0f, 1f) : 0f;
            int preferenceCount = Math.Max(0, _outputCount - 2);
            float[] preferences = preferenceCount > 0 ? new float[preferenceCount] : Array.Empty<float>();
            for (int i = 0; i < preferenceCount; i += 1)
            {
                preferences[i] = Math.Clamp(_outputs[i + 2], -1f, 1f);
            }

            return new BrainOutput
            {
                RotationDelta = rotationDelta,
                ForwardSpeed = forwardSpeed,
                ActionPreferenceVector = preferences
            };
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
