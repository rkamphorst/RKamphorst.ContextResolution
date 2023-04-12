using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.HttpApi.Dto;
using RKamphorst.ContextResolution.HttpApi.Test.Stubs;
using Xunit;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable StringLiteralTypo

namespace RKamphorst.ContextResolution.HttpApi.Test.HttpApiWithContext;

public class PostAsyncShould
{
    private readonly HttpApiWithContext<Parameter, Result> _sut;
    private readonly Mock<MockableHttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IContextProvider> _contextProviderMock;

    private static readonly Expression<Func<MockableHttpMessageHandler, Task<HttpResponseMessage>>>
        HttpMessageHandlerSendAsync = m =>
            m.MockableSendAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>());

    private static readonly ContextKey ContextAKey = ContextKey.FromNamedContext("ContextA");
    private static readonly ContextKey ContextBKey = ContextKey.FromNamedContext("ContextB");
    private static readonly ContextKey ContextCKey = ContextKey.FromNamedContext("ContextC");

    public PostAsyncShould()
    {
        _contextProviderMock = new Mock<IContextProvider>(MockBehavior.Strict);
        
        _httpMessageHandlerMock = new Mock<MockableHttpMessageHandler>
        {
            CallBase = true
        };

        var loggerMock = new Mock<ILogger<HttpApiWithContext<Parameter, Result>>>();
        _sut = HttpApiWithContext<Parameter, Result>.Create(
            new StubHttpClientFactory(_httpMessageHandlerMock.Object),
            "StubClientName",
            new Uri("https://http-api-with-context"),
            loggerMock.Object);


    }

    [Fact]
    public async Task PostRequestAndReturnSuccessResult()
    {
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result { IntResult = 66 };
        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContextDto<Parameter>>(requestContentString);
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result { IntResult = r!.Request!.IntParameter }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Once);
    }
    
    [Fact]
    public async Task ResolveRequestedContext()
    {
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B", "C" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), false,It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<object>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextC", It.IsAny<object>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContextDto<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(ContextAKey.Key) ?? false) ||
                    !(r.Context?.ContainsKey(ContextBKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext =
                                    new[]{ ContextAKey.Key, ContextBKey.Key}
                                    
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                if (!(r.Context?.ContainsKey(ContextCKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext = new [] { ContextCKey.Key }
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                    {
                        IntResult = r.Request!.IntParameter,
                        Contexts = r.Context.Values.Select(v => v.ToString()).ToArray()
                    }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextC", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(3));
    }

    [Fact]
    public async Task ResolveRequiredContext()
    {
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B", "C" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), true,It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<object>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextC", It.IsAny<object>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContextDto<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(ContextAKey.Key) ?? false) ||
                    !(r.Context?.ContainsKey(ContextBKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequireContext = 
                                    new[]{ ContextAKey.Key, ContextBKey.Key}
                                    
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                if (!(r.Context?.ContainsKey(ContextCKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext = new [] { ContextCKey.Key }
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                    {
                        IntResult = r.Request!.IntParameter,
                        Contexts = r.Context.Values.Select(v => v.ToString()).ToArray()
                    }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextC", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(3));
    }
    
 
    [Fact]
    public async Task ResolveMultipleRequestedContexts()
    {
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContextDto<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(ContextAKey.Key) ?? false) ||
                    !(r.Context?.ContainsKey(ContextBKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext =
                                    new[] { ContextAKey.Key, ContextBKey.Key }
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Request!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveSingleRequestedContext()
    {
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        
        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContextDto<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(ContextAKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext = new[] { ContextAKey.Key }
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Request!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveChainedRequestedContexts()
    {
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");
        
        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContextDto<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(ContextAKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext =
                                    new[] { ContextAKey.Key }
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                if (!(r.Context?.ContainsKey(ContextBKey.Key) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextDto
                            {
                                RequestContext = new[] { ContextBKey.Key }
                            }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Request!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(3));
    }
    
    [Fact]
    public async Task ThrowIfBadRequestResponseHasNoInformation()
    {
        var parameter = new Parameter { IntParameter = 66 };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");


        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .ReturnsAsync( (HttpRequestMessage req, CancellationToken _) => new HttpResponseMessage
            {
                Content = new StringContent("{}", Encoding.UTF8, MediaTypeNames.Application.Json),
                RequestMessage = req,
                StatusCode = HttpStatusCode.BadRequest,
            });
            
        Func<Task> act = 
            async () => await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Where(e => e.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ThrowIfBadRequestResponseCannotBeParsed()
    {
        var parameter = new Parameter { IntParameter = 66 };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");


        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .ReturnsAsync( (HttpRequestMessage req, CancellationToken _) => new HttpResponseMessage
            {
                Content = new StringContent("invalid json", Encoding.UTF8, MediaTypeNames.Application.Json),
                RequestMessage = req,
                StatusCode = HttpStatusCode.BadRequest,
            });
            
        Func<Task> act = 
            async () => await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Where(e => e.StatusCode == HttpStatusCode.BadRequest);
    }
    
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task ThrowIfResponseCodeIsUnrecognized(HttpStatusCode unknownStatusCode)
    {
        var parameter = new Parameter { IntParameter = 66 };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");


        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .ReturnsAsync( (HttpRequestMessage req, CancellationToken _) => new HttpResponseMessage
            {
                RequestMessage = req,
                StatusCode = unknownStatusCode,
            });
            
        Func<Task> act = 
            async () => await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Where(ex => ex.StatusCode == unknownStatusCode);
    }
    
    [Fact]
    public async Task ThrowIfSubmittedContextIsRequestedAgain()
    {
        var parameter = new Parameter { IntParameter = 66 };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<object>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");


        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .ReturnsAsync( (HttpRequestMessage req, CancellationToken _) => new HttpResponseMessage
            {
                Content =
                    JsonContent.Create(new NeedContextDto
                    {
                        RequestContext = new[]{ ContextAKey.Key }
                    }),
                RequestMessage = req,
                StatusCode = HttpStatusCode.BadRequest,
            });
            
        Func<Task> act = 
            async () => await _sut.PostAsync(parameter, _contextProviderMock.Object, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    
}