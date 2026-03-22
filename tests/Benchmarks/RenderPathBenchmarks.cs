using System;
using BenchmarkDotNet.Attributes;
using Spiffy.Monitoring;
using Spiffy.Monitoring.Config.Formatting;

namespace Benchmarks;

/// <summary>
/// Benchmarks isolating the Render/Dispose path with various configuration options.
/// Render is the most string-intensive operation: it converts all values to strings,
/// normalizes keys, encapsulates values, and joins everything into a delimited string.
/// </summary>
[MemoryDiagnoser]
[GcServer(true)]
public class RenderPathBenchmarks
{
    private Configuration _defaultConfig;
    private Configuration _logfmtConfig;
    private Configuration _removeNewlinesConfig;

    [GlobalSetup]
    public void Setup()
    {
        _defaultConfig = Configuration.Create(api =>
        {
            api.Providers.Add("noop", _ => { });
        });

        _logfmtConfig = Configuration.Create(api =>
        {
            api.Providers.Add("noop", _ => { });
            api.UseLogfmt();
        });

        _removeNewlinesConfig = Configuration.Create(api =>
        {
            api.Providers.Add("noop", _ => { });
            api.Formatting.Newlines(NewlineFormatting.Remove);
        });
    }

    [Benchmark(Baseline = true, Description = "Default config: 10 fields")]
    public void DefaultConfig_10Fields()
    {
        var ctx = new EventContext("Svc", "Op");
        PopulateTypical(ctx);
        ctx.Dispose();
    }

    [Benchmark(Description = "Logfmt config: 10 fields")]
    public void LogfmtConfig_10Fields()
    {
        var ctx = new EventContext("Svc", "Op");
        PopulateTypical(ctx);
        ctx.Dispose();
    }

    [Benchmark(Description = "Newline removal: multiline values")]
    public void NewlineRemoval()
    {
        var ctx = new EventContext("Svc", "Op");
        ctx["StackTrace"] = "at Foo.Bar()\r\n  at Baz.Qux()\r\n  at Program.Main()";
        ctx["Message"] = "Line1\nLine2\nLine3";
        ctx["Details"] = "First\r\nSecond\r\nThird\r\nFourth\r\nFifth";
        ctx.Dispose();
    }

    [Benchmark(Description = "Mixed: fields + timers + counts + encapsulation")]
    public void MixedWorkload()
    {
        var ctx = new EventContext("Svc", "Op");
        ctx["UserId"] = "user-12345";
        ctx["Query"] = "SELECT * FROM users WHERE id = 'test'";
        ctx["Endpoint"] = "/api/v1/items?page=1&size=10";
        using (ctx.Time("Database")) { }
        using (ctx.Time("Http")) { }
        ctx.Count("Retries");
        ctx.Count("Retries");
        ctx["field with spaces"] = "normalized";
        ctx["dotted.field"] = "also normalized";
        ctx.Dispose();
    }

    [Benchmark(Description = "Large payload: 50 fields")]
    public void LargePayload_50Fields()
    {
        var ctx = new EventContext("Svc", "Op");
        for (int i = 0; i < 50; i++)
        {
            ctx[$"Field{i}"] = $"Value{i}";
        }
        ctx.Dispose();
    }

    private static void PopulateTypical(EventContext ctx)
    {
        ctx["UserId"] = "user-12345";
        ctx["RequestId"] = "req-abcdef-1234-5678";
        ctx["Endpoint"] = "/api/v1/items";
        ctx["Method"] = "GET";
        ctx["StatusCode"] = 200;
        ctx["ContentLength"] = 4096;
        ctx["UserAgent"] = "Mozilla/5.0";
        ctx["RemoteIp"] = "192.168.1.100";
        ctx["Duration"] = 45.7;
        ctx["CacheHit"] = true;
    }
}
