namespace CasCap.Tests.Integration;

/// <summary>
/// Base class for <see cref="SignalCliRestClientService"/> integration tests.
/// Reads configuration from <c>appsettings.Test.json</c>, user secrets, and environment variables
/// and builds a minimal DI container that mirrors the production setup.
/// </summary>
public abstract class TestBase : IAsyncDisposable
{
    /// <summary>The xUnit output helper for writing test diagnostics.</summary>
    protected readonly ITestOutputHelper _output;

    /// <summary>The <see cref="SignalCliRestClientService"/> under test.</summary>
    protected readonly SignalCliRestClientService _svc;

    /// <summary>The resolved <see cref="SignalCliConfig"/> from configuration.</summary>
    protected readonly SignalCliConfig _config;

    /// <summary>The group name used for integration tests (from <c>CasCap:AIConfig:CommsAgent:Settings:GroupName</c>).</summary>
    protected readonly string _groupName;

    /// <summary>The DI service provider (exposed for sub-service resolution in derived tests).</summary>
    protected readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestBase"/> class.
    /// </summary>
    protected TestBase(ITestOutputHelper output)
    {
        _output = output;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddJsonFile("appsettings.Local.Test.json", optional: true)
            .AddUserSecrets<TestBase>()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);

        _config = services.AddAndGetCasCapConfiguration<SignalCliConfig>(configuration);
        services.AddSignalCli(configuration);

        _groupName = configuration["SignalCliTests:ExistingGroupName"] ?? string.Empty;

        _serviceProvider = services.BuildServiceProvider();
        _svc = _serviceProvider.GetRequiredService<SignalCliRestClientService>();
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
