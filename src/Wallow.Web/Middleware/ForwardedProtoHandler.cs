namespace Wallow.Web.Middleware;

/// <summary>
/// Adds X-Forwarded-Proto: https to outgoing requests so that OpenIddict on the API
/// accepts container-to-container HTTP calls as if they arrived through the reverse proxy.
/// </summary>
internal sealed class ForwardedProtoHandler : DelegatingHandler
{
    public ForwardedProtoHandler()
    {
        InnerHandler = new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        return base.SendAsync(request, cancellationToken);
    }
}
