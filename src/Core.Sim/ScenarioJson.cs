using System.Text.Json;

namespace Core.Sim;

public static class ScenarioJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<Scenario> Read(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string json = File.ReadAllText(path);
        IReadOnlyList<Scenario>? scenarios = JsonSerializer.Deserialize<IReadOnlyList<Scenario>>(json, Options);

        if (scenarios is null)
        {
            throw new InvalidOperationException("Unable to deserialize scenarios.");
        }

        return scenarios;
    }
}
