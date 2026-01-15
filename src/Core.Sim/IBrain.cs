using Core.Evo;

namespace Core.Sim;

public interface IBrain
{
    BrainOutput DecideAction(BrainInput input);
}
