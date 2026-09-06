using Benchmarks;

int durationSeconds = args.Length > 0 && int.TryParse(args[0], out var s) ? s : 60;

ThroughputHarness.Run(durationSeconds, "Baseline 6.4.7");
