namespace CasCap.Tests.Unit;

/// <summary>
/// Recorded details of a single outbound request, captured before the underlying
/// <see cref="HttpRequestMessage"/> is disposed by <c>HttpClientBase</c>.
/// </summary>
/// <param name="Method">The HTTP method used.</param>
/// <param name="Uri">The fully-resolved request URI.</param>
/// <param name="Body">The serialized request body, or <see langword="null"/> when there was none.</param>
/// <param name="Authorization">The <c>Authorization</c> header value, or <see langword="null"/> when unset.</param>
public sealed record RecordedCall(HttpMethod Method, Uri Uri, string? Body, string? Authorization)
{
    /// <summary>The request path and query, without scheme or authority.</summary>
    public string PathAndQuery => Uri.PathAndQuery;
}
