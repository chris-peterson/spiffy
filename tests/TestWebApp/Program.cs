using Microsoft.AspNetCore.Builder;
using Prometheus;
using Spiffy.Monitoring;
using Spiffy.Monitoring.Console;
using Spiffy.Monitoring.Prometheus;

Configuration.Initialize(spiffy =>
{
    spiffy.Providers
        .Console()
        .Prometheus();
});

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();

app.MapGet("/", () => "Hello World!");

app.UseEndpoints(endpoints => {
    endpoints.MapMetrics();
});

app.Run();
