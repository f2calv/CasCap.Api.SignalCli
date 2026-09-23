using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace CasCap.Tests.Unit;

/// <summary>Unit tests for <see cref="SignalCliConnectionHealthCheck"/>.</summary>
[Trait("Category", "Unit")]
public class SignalCliConnectionHealthCheckUnitTests
{
    [Fact]
    public async Task CheckHealthAsync_AcceptsNoContentByDefault()
    {
        // Regression for #4: the wrapper's default health endpoint returns 204, not 200.
        var config = new SignalCliConfig
        {
            BaseAddress = "http://localhost:8080",
            PhoneNumber = "+10000000000",
        };
        var handler = StubHttpMessageHandler.RespondStatus(HttpStatusCode.NoContent);
        using var client = new HttpClient(handler) { BaseAddress = new Uri(config.BaseAddress) };
        var sut = new SignalCliConnectionHealthCheck(
            NullLogger<SignalCliConnectionHealthCheck>.Instance,
            Options.Create(config),
            new TestHostEnvironment(),
            new StubHttpClientFactory(client));

        var result = await sut.CheckHealthAsync(
            new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = nameof(SignalCliConnectionHealthCheckUnitTests);

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}