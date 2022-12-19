using System.Net;
using System.Net.Mime;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.HttpApi.Dto;
using RKamphorst.ContextResolution.HttpApi.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.HttpApi.Test.HttpApiContextSource;

public class FillContextAsyncShould
{
    private readonly HttpApiContextSource<Parameter> _sut;
    private readonly Mock<IContextProvider> _contextProviderMock;
    private readonly Mock<MockableHttpMessageHandler> _httpMessageHandlerMock;

    public FillContextAsyncShould()
    {
        _contextProviderMock = new Mock<IContextProvider>();
        
        _httpMessageHandlerMock = new Mock<MockableHttpMessageHandler>
        {
            CallBase = true
        };

        var loggerMock = new Mock<ILogger<HttpApiContextSource<Parameter>>>();
        _sut = new HttpApiContextSource<Parameter>(new StubHttpClientFactory(_httpMessageHandlerMock.Object),
            loggerMock.Object);
    }

    [Fact]
    public async Task FillEmptyContext()
    {
        var expectCtx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": ""value"" }");
        _httpMessageHandlerMock
            .Setup(m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(@"{ ""property"": ""value"" }", Encoding.UTF8, MediaTypeNames.Application.Json),
                StatusCode = HttpStatusCode.OK,
            });
        
        var ctx = new JObject();
        await _sut.FillContextAsync(new Parameter { IntParameter = 66 },
            "https://context-source-uri", ctx, _contextProviderMock.Object, default);

        JToken.DeepEquals(ctx, expectCtx).Should().BeTrue();
    }
    
    [Fact]
    public async Task MergeWithExistingContextObject()
    {
        var expectCtx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": ""value"", ""FromSource"": 15 }");
        _httpMessageHandlerMock
            .Setup(m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(@"{ ""FromSource"": 15 }", Encoding.UTF8, MediaTypeNames.Application.Json),
                StatusCode = HttpStatusCode.OK,
            });
        
        var ctx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": ""value"" }")!;
        await _sut.FillContextAsync(new Parameter { IntParameter = 66 },
            "https://context-source-uri", ctx, _contextProviderMock.Object, default);

        JToken.DeepEquals(ctx, expectCtx).Should().BeTrue();
    }
    
    [Fact]
    public async Task MergeWithExistingContextArray()
    {
        var expectCtx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": [""value"", ""value2"" ]}");
        _httpMessageHandlerMock
            .Setup(m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(@"{ ""property"": [""value2""] }", Encoding.UTF8, MediaTypeNames.Application.Json),
                StatusCode = HttpStatusCode.OK,
            });
        
        var ctx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": [""value""] }")!;
        await _sut.FillContextAsync(new Parameter { IntParameter = 66 },
            "https://context-source-uri", ctx, _contextProviderMock.Object, default);

        JToken.DeepEquals(ctx, expectCtx).Should().BeTrue();
    }
    
    [Fact]
    public async Task MergeWithExistingContextDeepArray()
    {
        var expectCtx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": { ""nestedProperty"": [""value"", ""value2"" ] }}");
        _httpMessageHandlerMock
            .Setup(m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(@"{ ""property"": { ""nestedProperty"": [""value2"" ] }}", Encoding.UTF8, MediaTypeNames.Application.Json),
                StatusCode = HttpStatusCode.OK,
            });
        
        var ctx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": { ""nestedProperty"": [""value"" ] }}")!;
        await _sut.FillContextAsync(new Parameter { IntParameter = 66 },
            "https://context-source-uri", ctx, _contextProviderMock.Object, default);

        JToken.DeepEquals(ctx, expectCtx).Should().BeTrue();
    }
    
    [Fact]
    public async Task MergeWithExistingContextDeepArrayPreservingDuplicates()
    {
        var expectCtx =
            JsonConvert.DeserializeObject<JObject>(
                @"{ ""property"": { ""nestedProperty"": [""value"", ""value"", ""value2"" ] }}");
        _httpMessageHandlerMock
            .Setup(m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(@"{ ""property"": { ""nestedProperty"": [ ""value"", ""value2"" ] }}", Encoding.UTF8, MediaTypeNames.Application.Json),
                StatusCode = HttpStatusCode.OK,
            });
        
        var ctx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": { ""nestedProperty"": [""value"" ] }}")!;
        await _sut.FillContextAsync(new Parameter { IntParameter = 66 },
            "https://context-source-uri", ctx, _contextProviderMock.Object, default);

        JToken.DeepEquals(ctx, expectCtx).Should().BeTrue();
    }
    
    [Fact]
    public async Task RequestNeededContextThenFillContext()
    {
        
        var expectCtx = JsonConvert.DeserializeObject<JObject>(@"{ ""property"": ""value"", ""gotContext"": ""a"" }");

        _contextProviderMock
            .Setup(m => m.GetContextAsync("context", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((object)"a"));
        
        _httpMessageHandlerMock
            .Setup(m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async (HttpRequestMessage m, CancellationToken _) =>
            {
                var mContent = await m.Content!.ReadAsStringAsync(CancellationToken.None);
                var requestWithContext = JsonConvert.DeserializeObject<RequestWithContext<JObject>>(mContent)!;
                if (!requestWithContext.Context!.ContainsKey("context"))
                {
                    return new HttpResponseMessage
                    {
                        Content = new StringContent(@"{ ""needContext"": [ ""context"" ] }", Encoding.UTF8, MediaTypeNames.Application.Json),
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                return new HttpResponseMessage
                {
                    Content = new StringContent(
                        @"{ ""property"": ""value"", ""gotContext"": """ + requestWithContext.Context["context"] +
                        @""" }", Encoding.UTF8, MediaTypeNames.Application.Json),
                    StatusCode = HttpStatusCode.OK,
                };
            });
        
        var ctx = new JObject();
        await _sut.FillContextAsync(new Parameter { IntParameter = 66 },
            "https://context-source-uri", ctx, _contextProviderMock.Object, default);

        JToken.DeepEquals(ctx, expectCtx).Should().BeTrue();
        _httpMessageHandlerMock.Verify(
            m => m.MockableSendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _contextProviderMock.Verify(m => m.GetContextAsync("context", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}