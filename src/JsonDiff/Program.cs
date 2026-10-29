using System.Text.Json;
using JsonDiff.Core;

return Cli.Run(args, Console.Out, Console.Error);

public static class Cli
{
    private const string Usage = """
        jsondiff - structurally compare two JSON files.

        Usage:
          jsondiff <left.json> <right.json> [options]

        Options:
          --json                 output the diff as JSON instead of text
          --ignore-array-order   compare arrays as unordered multisets
          -h, --help             show this help

        Exit codes:
          0  no differences
          1  differences found
          2  usage or input error
        """;

    public static int Run(string[] args, TextWriter outWriter, TextWriter errWriter)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            outWriter.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var asJson = false;
        var options = new JsonDiffOptions();
        var positionals = new List<string>();

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--json":
                    asJson = true;
                    break;
                case "--ignore-array-order":
                    options = new JsonDiffOptions { IgnoreArrayOrder = true };
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        errWriter.WriteLine($"jsondiff: unknown option '{arg}'");
                        return 2;
                    }
                    positionals.Add(arg);
                    break;
            }
        }

        if (positionals.Count != 2)
        {
            errWriter.WriteLine("jsondiff: expected exactly two files to compare");
            errWriter.WriteLine(Usage);
            return 2;
        }

        string leftJson, rightJson;
        try
        {
            leftJson = File.ReadAllText(positionals[0]);
            rightJson = File.ReadAllText(positionals[1]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errWriter.WriteLine($"jsondiff: {ex.Message}");
            return 2;
        }

        IReadOnlyList<Change> changes;
        try
        {
            changes = JsonComparer.Compare(leftJson, rightJson, options);
        }
        catch (JsonException ex)
        {
            errWriter.WriteLine($"jsondiff: invalid JSON: {ex.Message}");
            return 2;
        }

        outWriter.WriteLine(asJson ? DiffReport.ToJson(changes) : DiffReport.ToText(changes));
        return changes.Count == 0 ? 0 : 1;
    }
}

// Exposed so the test project can drive the CLI in-process.
public partial class Program;
