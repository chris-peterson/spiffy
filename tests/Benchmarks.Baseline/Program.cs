using System;
using System.Diagnostics;
using Spiffy.Monitoring;

int durationSeconds = args.Length > 0 && int.TryParse(args[0], out var s) ? s : 60;

Configuration.Initialize(api =>
{
    api.Providers.Add("noop", _ => { });
});

// Warm up
for (int i = 0; i < 1000; i++)
    RunSingleIteration(i);

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

var memBefore = GC.GetTotalAllocatedBytes(precise: true);
long gen0Before = GC.CollectionCount(0);
long gen1Before = GC.CollectionCount(1);
long gen2Before = GC.CollectionCount(2);

long totalOps = 0;
var sw = Stopwatch.StartNew();
var deadline = TimeSpan.FromSeconds(durationSeconds);

int lastReportSec = 0;
while (sw.Elapsed < deadline)
{
    for (int i = 0; i < 1000; i++)
    {
        RunSingleIteration(totalOps + i);
    }
    totalOps += 1000;

    int elapsed = (int)sw.Elapsed.TotalSeconds;
    if (elapsed >= lastReportSec + 10)
    {
        lastReportSec = elapsed;
        Console.WriteLine($"  [{elapsed}s] {totalOps:N0} ops so far ({totalOps / sw.Elapsed.TotalSeconds:N0} ops/sec)");
    }
}

sw.Stop();

var memAfter = GC.GetTotalAllocatedBytes(precise: true);
long gen0After = GC.CollectionCount(0);
long gen1After = GC.CollectionCount(1);
long gen2After = GC.CollectionCount(2);

double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
double allocatedMB = (memAfter - memBefore) / 1024.0 / 1024.0;
double bytesPerOp = (double)(memAfter - memBefore) / totalOps;

Console.WriteLine();
Console.WriteLine("=== Throughput Results (Baseline 6.4.7) ===");
Console.WriteLine($"Duration:       {sw.Elapsed.TotalSeconds:F2}s");
Console.WriteLine($"Total ops:      {totalOps:N0}");
Console.WriteLine($"Throughput:     {opsPerSec:N0} ops/sec");
Console.WriteLine($"Avg latency:    {1_000_000.0 / opsPerSec:F2} us/op");
Console.WriteLine($"Allocated:      {allocatedMB:F1} MB ({bytesPerOp:F0} bytes/op)");
Console.WriteLine($"GC gen0:        {gen0After - gen0Before}");
Console.WriteLine($"GC gen1:        {gen1After - gen1Before}");
Console.WriteLine($"GC gen2:        {gen2After - gen2Before}");
Console.WriteLine();

static void RunSingleIteration(long i)
{
    var ctx = new EventContext("MyService", "ProcessRequest");

    ctx["RequestId"] = "req-abc-123-def-456";
    ctx["UserId"] = "user-78901";
    ctx["Endpoint"] = "/api/v1/items";
    ctx["Method"] = "GET";
    ctx["StatusCode"] = 200;

    using (ctx.Time("Database")) { }

    ctx.Count("ItemsReturned");
    ctx.Count("ItemsReturned");
    ctx.Count("ItemsReturned");

    if (i % 5 == 0)
    {
        ctx["Query"] = "SELECT * FROM users WHERE name = 'test'";
        ctx["Message"] = "Hello, world & goodbye";
    }

    if (i % 7 == 0)
    {
        ctx["field with spaces"] = "normalized";
        ctx["field.with.dots"] = "normalized";
    }

    if (i % 10 == 0)
    {
        ctx["LargePayload"] = true;
        ctx["Extra1"] = "extra-value-1";
        ctx["Extra2"] = "extra-value-2";
        ctx["Extra3"] = 42;
        ctx["Extra4"] = 99.9;
    }

    if (i % 20 == 0)
    {
        try
        {
            throw new InvalidOperationException("Something went wrong",
                new ArgumentException("Bad argument"));
        }
        catch (Exception ex)
        {
            ctx.IncludeException(ex);
        }
    }

    ctx.Dispose();
}
