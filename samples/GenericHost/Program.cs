var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSignalCli(builder.Configuration);
builder.Services.AddHostedService<SignalCliWorker>();

using var host = builder.Build();
await host.RunAsync();