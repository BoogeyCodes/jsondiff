using System.Text.Json;

namespace JsonDiff.Tests;

public sealed class CliTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    private string File(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"jsondiff-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    private (int Code, string Out, string Err) Run(params string[] args)
    {
        var outW = new StringWriter();
        var errW = new StringWriter();
        var code = Cli.Run(args, outW, errW);
        return (code, outW.ToString(), errW.ToString());
    }

    [Fact]
    public void Identical_files_exit_zero()
    {
        var a = File("""{"x":1}""");
        var b = File("""{"x":1}""");

        var (code, output, _) = Run(a, b);
        Assert.Equal(0, code);
        Assert.Contains("No differences.", output);
    }

    [Fact]
    public void Differing_files_exit_one_and_list_changes()
    {
        var a = File("""{"x":1}""");
        var b = File("""{"x":2}""");

        var (code, output, _) = Run(a, b);
        Assert.Equal(1, code);
        Assert.Contains("~ /x: 1 -> 2", output);
    }

    [Fact]
    public void Json_flag_emits_parseable_json()
    {
        var a = File("""{"x":1}""");
        var b = File("""{"x":2,"y":3}""");

        var (code, output, _) = Run(a, b, "--json");
        Assert.Equal(1, code);
        using var doc = JsonDocument.Parse(output);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void Ignore_array_order_flag_is_honoured()
    {
        var a = File("[1,2,3]");
        var b = File("[3,2,1]");

        Assert.Equal(1, Run(a, b).Code);                        // positional diff
        Assert.Equal(0, Run(a, b, "--ignore-array-order").Code); // multiset equal
    }

    [Fact]
    public void Wrong_argument_count_is_usage_error()
    {
        var a = File("""{"x":1}""");
        var (code, _, err) = Run(a);
        Assert.Equal(2, code);
        Assert.Contains("exactly two files", err);
    }

    [Fact]
    public void Unknown_option_is_usage_error()
    {
        var (code, _, err) = Run("--nope", "a", "b");
        Assert.Equal(2, code);
        Assert.Contains("unknown option", err);
    }

    [Fact]
    public void Missing_file_is_input_error()
    {
        var a = File("""{"x":1}""");
        var (code, _, err) = Run(a, Path.Combine(Path.GetTempPath(), "does-not-exist.json"));
        Assert.Equal(2, code);
        Assert.Contains("jsondiff:", err);
    }

    [Fact]
    public void Invalid_json_is_input_error()
    {
        var a = File("""{"x":1}""");
        var b = File("{ not valid json");
        var (code, _, err) = Run(a, b);
        Assert.Equal(2, code);
        Assert.Contains("invalid JSON", err);
    }

    [Fact]
    public void Help_exits_zero()
    {
        var (code, output, _) = Run("--help");
        Assert.Equal(0, code);
        Assert.Contains("Usage:", output);
    }

    [Fact]
    public void No_arguments_shows_usage_as_error()
    {
        var (code, output, _) = Run();
        Assert.Equal(2, code);
        Assert.Contains("Usage:", output);
    }

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try { System.IO.File.Delete(path); }
            catch (IOException) { /* best effort */ }
        }
    }
}
