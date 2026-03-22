using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Spiffy.Monitoring;

namespace Benchmarks;

/// <summary>
/// Benchmarks for the full EventContext lifecycle: create, populate, dispose (render).
/// This is the primary hot path in production systems.
/// </summary>
[MemoryDiagnoser]
[GcServer(true)]
public class EventContextLifecycleBenchmarks
{
    private Configuration _config;

    [GlobalSetup]
    public void Setup()
    {
        // Configure with a no-op provider so Render() runs but nothing is written
        _config = Configuration.Create(api =>
        {
            api.Providers.Add("noop", _ => { });
        });
    }

    [Benchmark(Description = "Minimal: create + dispose")]
    public void MinimalLifecycle()
    {
        using var ctx = new EventContext("TestComponent", "TestOperation");
    }

    [Benchmark(Description = "Typical: 5 string fields")]
    public void TypicalLifecycle_5Fields()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        ctx["UserId"] = "user-12345";
        ctx["RequestId"] = "req-abcdef";
        ctx["Endpoint"] = "/api/v1/items";
        ctx["Method"] = "GET";
        ctx["StatusCode"] = 200;
        ctx.Dispose();
    }

    [Benchmark(Description = "Heavy: 20 mixed fields")]
    public void HeavyLifecycle_20Fields()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        for (int i = 0; i < 10; i++)
        {
            ctx[$"StringField{i}"] = $"value-{i}";
        }
        for (int i = 0; i < 5; i++)
        {
            ctx[$"IntField{i}"] = i * 100;
        }
        for (int i = 0; i < 5; i++)
        {
            ctx[$"BoolField{i}"] = i % 2 == 0;
        }
        ctx.Dispose();
    }

    [Benchmark(Description = "With timers: 3 named timers")]
    public void LifecycleWithTimers()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        ctx["RequestId"] = "req-abcdef";
        using (ctx.Time("Database")) { }
        using (ctx.Time("Serialization")) { }
        using (ctx.Time("HttpCall")) { }
        ctx.Dispose();
    }

    [Benchmark(Description = "With counts: 5 counters")]
    public void LifecycleWithCounts()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        for (int i = 0; i < 5; i++)
        {
            ctx.Count("ItemsProcessed");
        }
        ctx.Count("Retries");
        ctx.Count("CacheMisses");
        ctx.Dispose();
    }

    [Benchmark(Description = "With exception")]
    public void LifecycleWithException()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        ctx["RequestId"] = "req-abcdef";
        try
        {
            throw new InvalidOperationException("Something went wrong",
                new ArgumentException("Bad argument"));
        }
        catch (Exception ex)
        {
            ctx.IncludeException(ex);
        }
        ctx.Dispose();
    }

    [Benchmark(Description = "With structure (10 properties)")]
    public void LifecycleWithStructure()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        ctx.IncludeStructure(new SampleStructure
        {
            Name = "TestItem",
            Count = 42,
            Price = 19.99m,
            IsActive = true,
            Created = DateTime.UtcNow,
            Category = "Electronics",
            Tags = "sale,featured",
            Rating = 4.5,
            Quantity = 100,
            Description = "A test item for benchmarking"
        });
        ctx.Dispose();
    }

    [Benchmark(Description = "Values needing encapsulation")]
    public void LifecycleWithEncapsulation()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        ctx["Query"] = "SELECT * FROM users WHERE name = 'test'";
        ctx["Message"] = "Hello, world & goodbye";
        ctx["Path"] = "/api/v1/items?page=1&size=10";
        ctx["Description"] = "This has \"quotes\" inside";
        ctx["Complex"] = "key=value, foo=bar & baz='qux'";
        ctx.Dispose();
    }

    [Benchmark(Description = "Keys needing normalization")]
    public void LifecycleWithKeyNormalization()
    {
        var ctx = new EventContext("TestComponent", "TestOperation");
        ctx["field with spaces"] = "value1";
        ctx["field.with.dots"] = "value2";
        ctx["  leading spaces"] = "value3";
        ctx["trailing spaces  "] = "value4";
        ctx["mixed.dots and spaces"] = "value5";
        ctx.Dispose();
    }

    public class SampleStructure
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public double Rating { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
    }
}
