using Core.Sim;
using Xunit;

namespace Core.Sim.Tests;

public class ReplayTests
{
    [Fact]
    public void Replay_Roundtrip_VerifyOk()
    {
        SimulationConfig config = CreateConfig();
        EpisodeRunner runner = new(config);
        IBrain brain = new FakeBrain();

        runner.RunEpisode(brain, seed: 123, ticks: 50, agentCount: 1, captureReplay: true, scenario: null, brainInfo: "fake");
        Replay replay = runner.LastReplay ?? throw new InvalidOperationException("Replay missing.");

        string json = ReplayJson.Serialize(replay);
        Replay parsed = ReplayJson.Deserialize(json);
        ReplayVerificationResult result = ReplayVerifier.Verify(parsed, config);

        Assert.True(result.Success);
    }

    [Fact]
    public void Replay_IsDeterministic_SameSeed()
    {
        SimulationConfig config = CreateConfig();

        EpisodeRunner firstRunner = new(config);
        firstRunner.RunEpisode(new FakeBrain(), seed: 999, ticks: 50, agentCount: 1, captureReplay: true, scenario: null, brainInfo: "fake");
        Replay firstReplay = firstRunner.LastReplay ?? throw new InvalidOperationException("Replay missing.");
        string firstJson = ReplayJson.Serialize(firstReplay);

        EpisodeRunner secondRunner = new(config);
        secondRunner.RunEpisode(new FakeBrain(), seed: 999, ticks: 50, agentCount: 1, captureReplay: true, scenario: null, brainInfo: "fake");
        Replay secondReplay = secondRunner.LastReplay ?? throw new InvalidOperationException("Replay missing.");
        string secondJson = ReplayJson.Serialize(secondReplay);

        Assert.Equal(firstJson, secondJson);
        Assert.Equal(firstReplay.FinalChecksum, secondReplay.FinalChecksum);
        Assert.Equal(firstReplay.Header.Ticks, secondReplay.Header.Ticks);
    }

    private static SimulationConfig CreateConfig()
    {
        return new SimulationConfig(
            Seed: 0,
            Dt: 1f,
            TicksPerEpisode: 100,
            WorldWidth: 8,
            WorldHeight: 8,
            AgentVisionRays: 4,
            AgentVisionRange: 4f,
            AgentFov: MathF.PI / 2f,
            AgentMaxSpeed: 1.5f,
            MoveDeadzone: 0.05f,
            ThirstRatePerSecond: 0.05f,
            DeathGraceSeconds: 2f);
    }

    private sealed class FakeBrain : IBrain
    {
        private int _step;

        public BrainOutput DecideAction(BrainInput input)
        {
            int step = _step;
            _step += 1;

            float moveX = step % 2 == 0 ? 1f : -1f;
            float moveY = step % 3 == 0 ? 0.5f : -0.25f;
            float drink = input.Thirst01 > 0.3f ? 0.8f : 0f;

            return new BrainOutput
            {
                MoveX = moveX,
                MoveY = moveY,
                ActionDrinkScore = drink
            };
        }
    }
}
