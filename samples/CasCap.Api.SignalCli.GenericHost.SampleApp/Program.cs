using CasCap.Common.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddLocalConfiguration(builder.Environment.EnvironmentName);

builder.Services.AddSignalCli(builder.Configuration);

builder.Services.AddHostedService<SignalCliWorker>();

using var host = builder.Build();
await host.RunAsync();
