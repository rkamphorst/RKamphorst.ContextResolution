using System.Net;

namespace RKamphorst.ContextResolution.HttpApi.Test.Stubs;

public class MockableHttpMessageHandler : HttpMessageHandler
{
    public virtual Task<HttpResponseMessage> MockableSendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return MockableSendAsync(request, cancellationToken);
    }
}