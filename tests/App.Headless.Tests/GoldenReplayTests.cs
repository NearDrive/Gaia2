using App.Headless;
using Xunit;

namespace App.Headless.Tests;

public class GoldenReplayTests
{
    [Fact]
    public void GoldenReplays_Verify()
    {
        string repoRoot = FindRepoRoot();
        string goldenDir = Path.Combine(repoRoot, "tests", "GoldenReplays");

        Assert.True(Directory.Exists(goldenDir), $"Golden replay directory missing: {goldenDir}");

        string[] replayPaths = Directory.GetFiles(goldenDir, "*.json");
        Assert.NotEmpty(replayPaths);

        foreach (string replayPath in replayPaths)
        {
            Program.ReplayVerificationResult result = Program.VerifyReplay(replayPath);
            string details = result.MismatchReason is not null
                ? $" Tick={result.MismatchTick?.ToString() ?? "unknown"} Reason={result.MismatchReason} Expected={result.ExpectedChecksum} Actual={result.ActualChecksum}"
                : $" Expected={result.ExpectedChecksum} Actual={result.ActualChecksum}";
            Assert.True(result.Verified, $"Replay verification failed for {replayPath}.{details}");
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string solutionPath = Path.Combine(current.FullName, "AgentsEvolutius2D.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located from AppContext.BaseDirectory.");
    }
}
