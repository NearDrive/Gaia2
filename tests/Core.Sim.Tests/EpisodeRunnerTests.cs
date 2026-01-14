using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class EpisodeRunnerTests
{
    [Fact]
    public void Episode_SameSeed_SameFitness()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new DrinkWhenPossibleBrain();

        EpisodeResult first = runner.RunEpisode(brain, 12345, 250, 1);
        EpisodeResult second = runner.RunEpisode(brain, 12345, 250, 1);

        Assert.Equal(first.Fitness, second.Fitness);
    }

    [Fact]
    public void Episode_DifferentSeed_DifferentChecksum()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new DrinkWhenPossibleBrain();

        EpisodeResult first = runner.RunEpisode(brain, 111, 250, 1);
        EpisodeResult second = runner.RunEpisode(brain, 222, 250, 1);

        Assert.NotEqual(first.Checksum, second.Checksum);
    }

    [Fact]
    public void Fitness_RewardsSurvival()
    {
        SimulationConfig config = CreateConfig(thirstRate: 1f, deathGrace: 2f);
        EpisodeRunner runner = new(config);

        EpisodeResult noDrink = runner.RunEpisode(new NoDrinkBrain(), 42, 50, 1);
        EpisodeResult drink = runner.RunEpisode(new DrinkWhenPossibleBrain(), 42, 50, 1);

        Assert.True(drink.Fitness > noDrink.Fitness);
    }

    [Fact]
    public void Episode_StopsWhenDead()
    {
        SimulationConfig config = CreateConfig(thirstRate: 1f, deathGrace: 0.5f);
        EpisodeRunner runner = new(config);
        EpisodeResult result = runner.RunEpisode(new NoDrinkBrain(), 99, 50, 1);

        Assert.True(result.TicksSurvived < result.TicksRequested);
    }

    private static SimulationConfig CreateConfig(float thirstRate = 0.05f, float deathGrace = 2f)
    {
        return new SimulationConfig(
            0,
            1f,
            100,
            1,
            1,
            4,
            4f,
            MathF.PI / 2f,
            thirstRate,
            deathGrace);
    }

    private sealed class NoDrinkBrain : IBrain
    {
        public AgentAction DecideAction(AgentState agent, Simulation simulation)
        {
            return AgentAction.None;
        }
    }

    private sealed class DrinkWhenPossibleBrain : IBrain
    {
        public AgentAction DecideAction(AgentState agent, Simulation simulation)
        {
            if (!agent.IsAlive)
            {
                return AgentAction.None;
            }

            return Simulation.CanDrink(simulation.World, agent.Position)
                ? AgentAction.Drink
                : AgentAction.None;
        }
    }
}
