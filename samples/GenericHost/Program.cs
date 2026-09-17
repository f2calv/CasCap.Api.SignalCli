using Microsoft.Extensions.Configuration;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

//Added explicitly rather than relying on the Development-only default, so the same secrets.json
//is read when the sample runs from a container under a non-Development environment name.
//Environment variables are re-added afterwards to restore their usual precedence over secrets.
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSignalCli(builder.Configuration);

//Sample-only voice speech-to-text acceptance harness; disabled unless VoiceSttHarness:Enabled is set.
builder.Services.Configure<VoiceSttHarnessConfig>(
    builder.Configuration.GetSection(VoiceSttHarnessConfig.ConfigurationSectionName));
builder.Services.AddHttpClient(VoiceSttHarnessConfig.HttpClientName, (sp, client) =>
    client.Timeout = TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<VoiceSttHarnessConfig>>().Value.TimeoutMs));
builder.Services.AddSingleton<WhisperCppVoiceTranscriber>();
builder.Services.AddSingleton<WhisperAsrVoiceTranscriber>();
builder.Services.AddSingleton<IVoiceTranscriber>(sp =>
    sp.GetRequiredService<IOptions<VoiceSttHarnessConfig>>().Value.Provider switch
    {
        VoiceSttProvider.WhisperCpp => sp.GetRequiredService<WhisperCppVoiceTranscriber>(),
        _ => sp.GetRequiredService<WhisperAsrVoiceTranscriber>()
    });
builder.Services.AddSingleton<FfmpegAudioTranscoder>();

builder.Services.AddHostedService<SignalCliWorker>();

using var host = builder.Build();
await host.RunAsync();
