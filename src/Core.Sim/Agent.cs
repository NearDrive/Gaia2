using Core.Evo;

namespace Core.Sim;

public sealed class Agent
{
    public Agent(AgentState state, IBrain brain)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Brain = brain ?? throw new ArgumentNullException(nameof(brain));
    }

    public AgentState State { get; }

    public IBrain Brain { get; }
}
