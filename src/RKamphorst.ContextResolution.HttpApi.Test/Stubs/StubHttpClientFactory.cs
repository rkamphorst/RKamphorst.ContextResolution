namespace RKamphorst.ContextResolution.HttpApi.Test.Stubs;

public class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _messageHandler;

    public StubHttpClientFactory(HttpMessageHandler messageHandler)
    {
        _messageHandler = messageHandler;
    }
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_messageHandler);
    }
}