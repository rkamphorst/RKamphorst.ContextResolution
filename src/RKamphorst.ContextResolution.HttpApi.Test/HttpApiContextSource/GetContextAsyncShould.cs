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

public class GetContextAsyncShould
{
    private readonly HttpApiNamedContextSource _sut;
    private readonly Mock<IContextProvider> _contextProviderMock;
    private readonly Mock<MockableHttpMessageHandler> _httpMessageHandlerMock;

    public GetContextAsyncShould()
    {
        _contextProviderMock = new Mock<IContextProvider>();

        _httpMessageHandlerMock = new Mock<MockableHttpMessageHandler>
        {
            CallBase = true
        };

        var loggerMock = new Mock<ILogger<HttpApiNamedContextSource>>();
        _sut = new HttpApiNamedContextSource(new StubHttpClientFactory(_httpMessageHandlerMock.Object),
            new Uri("https://context-source-uri"), loggerMock.Object);
    }

    [Fact]
    public async Task ReturnResult()
    {
        ContextKey contextKey = ContextKey.FromNamedContext("ctx");

        _httpMessageHandlerMock
            .Setup(
                m => m.MockableSendAsync(
                    It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(
                        new ContextResultsDto
                        {
                            Results = new[]
                            {
                                new ContextResultDto
                                {
                                    Result = new { property = "value" }
                                }
                            }
                        }), Encoding.UTF8, MediaTypeNames.Application.Json
                ),
                StatusCode = HttpStatusCode.OK
            });

        ContextResult[] gotResults =
            await _sut.GetContextAsync(contextKey, _contextProviderMock.Object, default);

        gotResults.Should().HaveCount(1);
        gotResults[0].Name.Should().Be((ContextName)"ctx");
        gotResults[0].GetResult().Should().BeEquivalentTo(new JObject { ["property"] = "value" });
        gotResults[0].CacheInstruction.Should().Be((CacheInstruction)"transient");
    }

    [Fact]
    public async Task RequestNeededContextThenFillContext()
    {

        ContextKey contextKey = ContextKey.FromNamedContext("ctx");

        _contextProviderMock
            .Setup(
                m => m.GetContextAsync(
                    "ctx", It.IsAny<object>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult((object)"a"));

        _httpMessageHandlerMock
            .Setup(
                m => m.MockableSendAsync(
                    It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()
                )
            )
            .Returns(async (HttpRequestMessage m, CancellationToken _) =>
            {
                var mContent = await m.Content!.ReadAsStringAsync(CancellationToken.None);
                var requestWithContext = JsonConvert.DeserializeObject<RequestWithContextDto<JObject>>(mContent)!;
                if (!requestWithContext.Context!.ContainsKey((string)contextKey))
                {
                    return new HttpResponseMessage
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new NeedContextDto
                        {
                            RequestContext = new[] { (string)contextKey }
                        }), Encoding.UTF8, MediaTypeNames.Application.Json),
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                return new HttpResponseMessage
                {
                    Content = new StringContent(JsonConvert.SerializeObject(
                            new ContextResultsDto
                            {
                                Results = new[]
                                {
                                    new ContextResultDto
                                    {
                                        Result = new { property = "value" }
                                    }
                                }
                            }), Encoding.UTF8, MediaTypeNames.Application.Json
                    ),
                    StatusCode = HttpStatusCode.OK
                };
            });

        ContextResult[] gotResults =
            await _sut.GetContextAsync(contextKey, _contextProviderMock.Object, default);

        gotResults.Should().HaveCount(1);
        gotResults[0].Name.Should().Be((ContextName)"ctx");
        gotResults[0].GetResult().Should().BeEquivalentTo(new JObject { ["property"] = "value" });
        gotResults[0].CacheInstruction.Should().Be((CacheInstruction)"transient");

        _httpMessageHandlerMock.Verify(
            m => m.MockableSendAsync(
                It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()
            ),
            Times.Exactly(2)
        );
        _contextProviderMock.Verify(m => m.GetContextAsync(
                "ctx",
                It.IsAny<object>(),
                false,
                It.IsAny<CancellationToken>()), Times.Once
        );
    }
}