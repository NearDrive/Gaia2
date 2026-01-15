using Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Core.Sim.Tests;

public class EpisodeRunnerTests
{
    private readonly ITestOutputHelper _output;

    public EpisodeRunnerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Episode_SameSeed_SameFitness()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new AlwaysDrinkBrain();

        EpisodeResult first = runner.RunEpisode(brain, 12345, 250, 1);
        EpisodeResult second = runner.RunEpisode(brain, 12345, 250, 1);

        Assert.Equal(first.Fitness, second.Fitness);
    }

    [Fact]
    public void Episode_DifferentSeed_DifferentChecksum()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new AlwaysDrinkBrain();

        EpisodeResult first = runner.RunEpisode(brain, 111, 250, 1);
        EpisodeResult second = runner.RunEpisode(brain, 222, 250, 1);

        Assert.NotEqual(first.Checksum, second.Checksum);
    }

    [Fact]
    public void Fitness_RewardsSurvival()
    {
        SimulationConfig config = CreateConfig(thirstRate: 1f, deathGrace: 2f);
        EpisodeRunner runner = new(config);

        EpisodeResult rBad = runner.RunEpisode(new NoDrinkBrain(), 42, 50, 1);
        EpisodeResult rGood = runner.RunEpisode(new AlwaysDrinkBrain(), 42, 50, 1);

        _output.WriteLine($"Bad: Ticks={rBad.TicksSurvived} Drinks={rBad.SuccessfulDrinks} Thirst={rBad.AvgThirst01:0.000000} Fitness={rBad.Fitness:0.000}");
        _output.WriteLine($"Good: Ticks={rGood.TicksSurvived} Drinks={rGood.SuccessfulDrinks} Thirst={rGood.AvgThirst01:0.000000} Fitness={rGood.Fitness:0.000}");

        Assert.True(rGood.Fitness > rBad.Fitness);
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

    private sealed class AlwaysDrinkBrain : IBrain
    {
        public AgentAction DecideAction(AgentState agent, Simulation simulation)
        {
            if (!agent.IsAlive)
            {
                return AgentAction.None;
            }

            return AgentAction.Drink;
        }
    }
}
