using BenchmarkDotNet.Attributes;
using Spiffy.Monitoring;

namespace Benchmarks;

/// <summary>
/// Micro-benchmarks for StringExtensions methods.
/// These are called per-key and per-value during Render(), so they are in the innermost loop.
/// </summary>
[MemoryDiagnoser]
[GcServer(true)]
public class StringExtensionsBenchmarks
{
    // --- ContainsWhiteSpace ---

    [Benchmark(Description = "ContainsWhiteSpace: no whitespace")]
    public bool ContainsWhiteSpace_None() => "ComponentName".ContainsWhiteSpace();

    [Benchmark(Description = "ContainsWhiteSpace: has space")]
    public bool ContainsWhiteSpace_Space() => "Component Name".ContainsWhiteSpace();

    [Benchmark(Description = "ContainsWhiteSpace: has tab")]
    public bool ContainsWhiteSpace_Tab() => "Component\tName".ContainsWhiteSpace();

    [Benchmark(Description = "ContainsWhiteSpace: long key no whitespace")]
    public bool ContainsWhiteSpace_LongKey() => "VeryLongFieldNameThatHasNoWhitespaceAnywhere".ContainsWhiteSpace();

    // --- RemoveWhiteSpace ---

    [Benchmark(Description = "RemoveWhiteSpace: simple")]
    public string RemoveWhiteSpace_Simple() => "field name".RemoveWhiteSpace();

    [Benchmark(Description = "RemoveWhiteSpace: multiple spaces")]
    public string RemoveWhiteSpace_Multiple() => "field  with   multiple    spaces".RemoveWhiteSpace();

    [Benchmark(Description = "RemoveWhiteSpace: no whitespace (noop)")]
    public string RemoveWhiteSpace_NoOp() => "FieldName".RemoveWhiteSpace();

    // --- RequiresEncapsulation ---

    [Benchmark(Description = "RequiresEncapsulation: simple value")]
    public bool RequiresEncapsulation_Simple()
    {
        return "simplevalue".RequiresEncapsulation(out _);
    }

    [Benchmark(Description = "RequiresEncapsulation: value with spaces")]
    public bool RequiresEncapsulation_Spaces()
    {
        return "value with spaces".RequiresEncapsulation(out _);
    }

    [Benchmark(Description = "RequiresEncapsulation: value with quotes")]
    public bool RequiresEncapsulation_Quotes()
    {
        return "value \"with\" quotes".RequiresEncapsulation(out _);
    }

    [Benchmark(Description = "RequiresEncapsulation: long value no special chars")]
    public bool RequiresEncapsulation_LongNoSpecial()
    {
        return "ThisIsAVeryLongValueThatDoesNotContainAnySpecialCharactersAtAll12345".RequiresEncapsulation(out _);
    }

    [Benchmark(Description = "RequiresEncapsulation: URL with special chars")]
    public bool RequiresEncapsulation_Url()
    {
        return "https://example.com/api?key=value&other=123".RequiresEncapsulation(out _);
    }

    // --- WrappedInQuotes ---

    [Benchmark(Description = "WrappedInQuotes: short value")]
    public string WrappedInQuotes_Short() => "hello".WrappedInQuotes('"');

    [Benchmark(Description = "WrappedInQuotes: medium value")]
    public string WrappedInQuotes_Medium() => "This is a medium-length value for testing".WrappedInQuotes('"');
}
