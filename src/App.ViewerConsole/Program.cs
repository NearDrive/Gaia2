using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Sim;

namespace App.ViewerConsole;

public static class Program
{
    public static int Main(string[] args)
    {
        ViewerOptions options;

        try
        {
            options = ViewerOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            if (ex.Message == "Help requested.")
            {
                WriteUsage();
                return 0;
            }

            Console.Error.WriteLine(ex.Message);
            WriteUsage();
            return 1;
        }

        if (options.StreamPath is null && !Directory.Exists(options.InputDirectory ?? string.Empty))
        {
            Console.Error.WriteLine($"Input directory '{options.InputDirectory}' does not exist.");
            return 1;
        }

        if (options.StreamPath is not null && !File.Exists(options.StreamPath))
        {
            Console.Error.WriteLine($"Snapshot stream file '{options.StreamPath}' does not exist.");
            return 1;
        }

        IReadOnlyList<SnapshotEntry> snapshots = options.StreamPath is null
            ? SnapshotLoader.Load(options.InputDirectory ?? string.Empty)
            : SnapshotLoader.LoadStream(options.StreamPath);

        if (snapshots.Count == 0)
        {
            Console.Error.WriteLine(options.StreamPath is null
                ? "No snapshot files found (expected snap_*.json)."
                : "No snapshots found in stream file.");
            return 1;
        }

        int startIndex = SnapshotLoader.FindStartIndex(snapshots, options.StartTick);

        Console.CursorVisible = false;
        try
        {
            ViewerLoop.Run(snapshots, startIndex, options.Mode, options.Fps);
        }
        finally
        {
            Console.CursorVisible = true;
        }

        return 0;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/App.ViewerConsole -- --in <dir> [--fps <int>] [--mode step|play] [--start <tick>]");
        Console.WriteLine("  dotnet run --project src/App.ViewerConsole -- --stream <file> [--fps <int>] [--mode step|play] [--start <tick>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --in <dir>       Directory containing snap_*.json (mutually exclusive with --stream)");
        Console.WriteLine("  --stream <file>  JSONL snapshot stream file (mutually exclusive with --in)");
        Console.WriteLine("  --fps <int>      Frames per second in play mode (default 10)");
        Console.WriteLine("  --mode <mode>    step or play (default step)");
        Console.WriteLine("  --start <tick>   Start at or after this tick (default lowest)");
    }
}

internal enum ViewerMode
{
    Step,
    Play
}

internal sealed record ViewerOptions(string? InputDirectory, string? StreamPath, int Fps, ViewerMode Mode, int? StartTick)
{
    public static ViewerOptions Parse(string[] args)
    {
        string? inputDirectory = null;
        string? streamPath = null;
        int fps = 10;
        ViewerMode mode = ViewerMode.Step;
        int? startTick = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--in":
                    inputDirectory = ReadValue(args, ref i, "--in");
                    break;
                case "--stream":
                    streamPath = ReadValue(args, ref i, "--stream");
                    break;
                case "--fps":
                    fps = ParseInt(ReadValue(args, ref i, "--fps"), "--fps", minValue: 1);
                    break;
                case "--mode":
                    mode = ParseMode(ReadValue(args, ref i, "--mode"));
                    break;
                case "--start":
                    startTick = ParseInt(ReadValue(args, ref i, "--start"), "--start", minValue: 0);
                    break;
                case "-h":
                case "--help":
                    throw new ArgumentException("Help requested.");
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(inputDirectory) && !string.IsNullOrWhiteSpace(streamPath))
        {
            throw new ArgumentException("--in and --stream are mutually exclusive.");
        }

        if (string.IsNullOrWhiteSpace(inputDirectory) && string.IsNullOrWhiteSpace(streamPath))
        {
            throw new ArgumentException("Missing required --in <dir> or --stream <file> argument.");
        }

        return new ViewerOptions(inputDirectory, streamPath, fps, mode, startTick);
    }

    private static string ReadValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index++;
        return args[index];
    }

    private static int ParseInt(string value, string name, int minValue)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < minValue)
        {
            throw new ArgumentException($"{name} must be an integer >= {minValue}.");
        }

        return result;
    }

    private static ViewerMode ParseMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "step" => ViewerMode.Step,
            "play" => ViewerMode.Play,
            _ => throw new ArgumentException("--mode must be 'step' or 'play'.")
        };
    }
}

internal static class SnapshotLoader
{
    private static readonly Regex SnapshotRegex = new("^snap_(\\d+)\\.json$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<SnapshotEntry> Load(string directory)
    {
        List<SnapshotEntry> entries = new();

        foreach (string file in Directory.GetFiles(directory, "snap_*.json"))
        {
            string fileName = Path.GetFileName(file);
            Match match = SnapshotRegex.Match(fileName);

            if (!match.Success)
            {
                continue;
            }

            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tick))
            {
                continue;
            }

            string json = File.ReadAllText(file);
            WorldSnapshot snapshot = SnapshotJson.Deserialize(json);
            entries.Add(new SnapshotEntry(tick, snapshot, file));
        }

        return entries.OrderBy(entry => entry.Tick).ToList();
    }

    public static IReadOnlyList<SnapshotEntry> LoadStream(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Snapshot stream file not found: {path}");
        }

        using StreamReader reader = new(path);
        string? headerLine = reader.ReadLine();

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidOperationException("Snapshot stream header line is missing.");
        }

        JsonSerializerOptions options = SnapshotJson.GetReadOptions();
        SnapshotStreamHeader? header = JsonSerializer.Deserialize<SnapshotStreamHeader>(headerLine, options);

        if (header is null)
        {
            throw new InvalidOperationException("Unable to deserialize snapshot stream header.");
        }

        List<SnapshotEntry> entries = new();

        while (true)
        {
            string? line = reader.ReadLine();

            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                continue;
            }

            WorldSnapshot snapshot = SnapshotJson.Deserialize(line);
            entries.Add(new SnapshotEntry(snapshot.Header.Tick, snapshot, path));
        }

        return entries.OrderBy(entry => entry.Tick).ToList();
    }

    public static int FindStartIndex(IReadOnlyList<SnapshotEntry> entries, int? startTick)
    {
        if (!startTick.HasValue)
        {
            return 0;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Tick >= startTick.Value)
            {
                return i;
            }
        }

        return entries.Count - 1;
    }
}

internal sealed record SnapshotEntry(int Tick, WorldSnapshot Snapshot, string Path);

internal static class ViewerLoop
{
    public static void Run(IReadOnlyList<SnapshotEntry> snapshots, int startIndex, ViewerMode mode, int fps)
    {
        int index = startIndex;
        bool playing = mode == ViewerMode.Play;
        TimeSpan frameDuration = TimeSpan.FromSeconds(1.0 / fps);
        int lastIndex = snapshots.Count - 1;

        while (true)
        {
            SnapshotEntry entry = snapshots[index];
            SnapshotRenderer.Render(entry.Snapshot, entry.Tick, index, snapshots.Count, playing, fps);

            if (playing)
            {
                DateTime frameEnd = DateTime.UtcNow + frameDuration;

                while (DateTime.UtcNow < frameEnd)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                        if (!HandleKey(key, ref playing, ref index, lastIndex))
                        {
                            return;
                        }
                    }

                    Thread.Sleep(10);
                }

                if (playing)
                {
                    if (index < lastIndex)
                    {
                        index++;
                    }
                    else
                    {
                        playing = false;
                    }
                }
            }
            else
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (!HandleKey(key, ref playing, ref index, lastIndex))
                {
                    return;
                }
            }
        }
    }

    private static bool HandleKey(ConsoleKeyInfo key, ref bool playing, ref int index, int lastIndex)
    {
        switch (key.Key)
        {
            case ConsoleKey.Q:
                return false;
            case ConsoleKey.Spacebar:
                playing = !playing;
                return true;
            case ConsoleKey.N:
                if (!playing && index < lastIndex)
                {
                    index++;
                }

                return true;
            case ConsoleKey.P:
                if (!playing && index > 0)
                {
                    index--;
                }

                return true;
            default:
                return true;
        }
    }
}

internal static class SnapshotRenderer
{
    public static void Render(WorldSnapshot snapshot, int tick, int index, int total, bool playing, int fps)
    {
        Console.Clear();
        Console.WriteLine($"Tick: {tick}  Agents: {snapshot.Header.AgentCount}  Checksum: {snapshot.Header.WorldChecksum}");
        Console.WriteLine($"Frame: {index + 1}/{total}  Mode: {(playing ? "play" : "step")}  FPS: {fps}");

        if (playing)
        {
            Console.WriteLine("Controls: space pause, q quit");
        }
        else
        {
            Console.WriteLine("Controls: n next, p prev, space play, q quit");
        }

        int width = snapshot.Header.WorldWidth;
        int height = snapshot.Header.WorldHeight;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        char[,] buffer = new char[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer[y, x] = '.';
            }
        }

        foreach (AgentSnapshot agent in snapshot.Agents)
        {
            int x = (int)MathF.Floor(agent.X);
            int y = (int)MathF.Floor(agent.Y);

            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                continue;
            }

            char marker = agent.Alive ? '@' : 'x';

            if (buffer[y, x] == '@')
            {
                continue;
            }

            if (buffer[y, x] == 'x' && marker == 'x')
            {
                continue;
            }

            if (buffer[y, x] == 'x' && marker == '@')
            {
                buffer[y, x] = '@';
                continue;
            }

            buffer[y, x] = marker;
        }

        for (int y = 0; y < height; y++)
        {
            char[] row = new char[width];
            for (int x = 0; x < width; x++)
            {
                row[x] = buffer[y, x];
            }

            Console.WriteLine(row);
        }
    }
}
