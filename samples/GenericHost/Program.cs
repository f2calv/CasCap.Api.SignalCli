var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSignalCli(builder.Configuration);

//Sample-only voice speech-to-text acceptance harness; disabled unless VoiceSttHarness:Enabled is set.
builder.Services.Configure<VoiceSttHarnessConfig>(
    builder.Configuration.GetSection(VoiceSttHarnessConfig.ConfigurationSectionName));
builder.Services.AddHttpClient(WhisperCppVoiceTranscriber.HttpClientName, (sp, client) =>
    client.Timeout = TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<VoiceSttHarnessConfig>>().Value.TimeoutMs));
builder.Services.AddSingleton<IVoiceTranscriber, WhisperCppVoiceTranscriber>();
builder.Services.AddSingleton<FfmpegAudioTranscoder>();

builder.Services.AddHostedService<SignalCliWorker>();

using var host = builder.Build();
await host.RunAsync();