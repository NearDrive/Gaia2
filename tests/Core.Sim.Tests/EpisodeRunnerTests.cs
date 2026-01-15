using System.Numerics;
using Core.Evo;
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
        Assert.Equal(first.VisitedCells, second.VisitedCells);
        Assert.Equal(first.DrinkableCellsVisited, second.DrinkableCellsVisited);
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
        SimulationConfig config = CreateConfig(thirstRate: 0.2f, deathGrace: 2f);
        EpisodeRunner runner = new(config);
        Vector2 startPosition = FindWaterStart(config, 42);

        EpisodeResult rBad = runner.RunEpisode(new NoDrinkBrain(), 42, 50, new[] { startPosition });
        EpisodeResult rGood = runner.RunEpisode(new AlwaysDrinkBrain(), 42, 50, new[] { startPosition });

        _output.WriteLine($"Bad: Ticks={rBad.TicksSurvived} Drinks={rBad.SuccessfulDrinks} Thirst={rBad.AvgThirst01:0.000000} Fitness={rBad.Fitness:0.000}");
        _output.WriteLine($"Good: Ticks={rGood.TicksSurvived} Drinks={rGood.SuccessfulDrinks} Thirst={rGood.AvgThirst01:0.000000} Fitness={rGood.Fitness:0.000}");

        Assert.True(rGood.Fitness > rBad.Fitness);
    }

    [Fact]
    public void ExplorationScore_Increases_WhenVisitingNewCells()
    {
        SimulationConfig config = CreateConfig(thirstRate: 0f, deathGrace: 100f);
        EpisodeRunner runner = new(config);
        Vector2 startPosition = new(1.5f, 1.5f);

        EpisodeResult stationary = runner.RunEpisode(new StationaryBrain(), 7, 20, new[] { startPosition });
        EpisodeResult explorer = runner.RunEpisode(new StraightLineBrain(), 7, 20, new[] { startPosition });

        _output.WriteLine($"Stationary Visited={stationary.VisitedCells} Explorer Visited={explorer.VisitedCells}");

        Assert.True(explorer.VisitedCells > stationary.VisitedCells);
    }

    [Fact]
    public void Fitness_StillRewardsSurvival_MoreThanExploration()
    {
        SimulationConfig config = CreateConfig(thirstRate: 0.5f, deathGrace: 0.5f);
        EpisodeRunner runner = new(config);
        Vector2 startPosition = FindWaterStart(config, 15);

        EpisodeResult explorer = runner.RunEpisode(new StraightLineBrain(), 15, 50, new[] { startPosition });
        EpisodeResult survivor = runner.RunEpisode(new AlwaysDrinkBrain(), 15, 50, new[] { startPosition });

        _output.WriteLine($"Explorer: Ticks={explorer.TicksSurvived} Visited={explorer.VisitedCells} Fitness={explorer.Fitness:0.000}");
        _output.WriteLine($"Survivor: Ticks={survivor.TicksSurvived} Visited={survivor.VisitedCells} Fitness={survivor.Fitness:0.000}");

        Assert.True(survivor.Fitness > explorer.Fitness);
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
            8,
            8,
            4,
            4f,
            MathF.PI / 2f,
            1.5f,
            0.05f,
            thirstRate,
            deathGrace);
    }

    private static Vector2 FindWaterStart(SimulationConfig config, int seed)
    {
        GridWorld world = new(config.WorldWidth, config.WorldHeight, seed);
        for (int y = 0; y < world.Height; y += 1)
        {
            for (int x = 0; x < world.Width; x += 1)
            {
                if (world.GetTile(x, y).Id == TileId.Water)
                {
                    return new Vector2(x + 0.5f, y + 0.5f);
                }
            }
        }

        throw new InvalidOperationException("No water tile found in test world.");
    }

    private sealed class NoDrinkBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput();
        }
    }

    private sealed class AlwaysDrinkBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput
            {
                ActionDrinkScore = 1f
            };
        }
    }

    private sealed class StationaryBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput();
        }
    }

    private sealed class StraightLineBrain : IBrain
    {
        public BrainOutput DecideAction(BrainInput input)
        {
            return new BrainOutput
            {
                MoveX = 1f
            };
        }
    }
}
