namespace Core.Sim;

public interface IBrain
{
    AgentAction DecideAction(AgentState agent, Simulation simulation);
}
