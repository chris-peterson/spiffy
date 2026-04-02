using BenchmarkDotNet.Running;
using Benchmarks;

if (args.Length > 0 && args[0] == "throughput")
{
    int seconds = args.Length > 1 && int.TryParse(args[1], out var s) ? s : 60;
    ThroughputHarness.Run(seconds);
}
else
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
