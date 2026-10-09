namespace CasCap.Tests.Unit;

/// <summary>
/// Returns a single pre-built <see cref="HttpClient"/> regardless of the requested name.
/// </summary>
/// <param name="client">The client to hand out.</param>
public sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    /// <inheritdoc/>
    public HttpClient CreateClient(string name) => client;
}