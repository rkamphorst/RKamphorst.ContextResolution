using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
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

    public PostAsyncShould()
    {
        _contextProviderMock = new Mock<IContextProvider>();
        
        _httpMessageHandlerMock = new Mock<MockableHttpMessageHandler>
        {
            CallBase = true
        };

        var loggerMock = new Mock<ILogger<HttpApiWithContext<Parameter, Result>>>();
        _sut = new HttpApiWithContext<Parameter, Result>(
            new StubHttpClientFactory(_httpMessageHandlerMock.Object),
            loggerMock.Object);
    }

    [Fact]
    public async Task PostRequestAndReturnSuccessResult()
    {
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result { IntResult = 66 };
        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContext<Parameter>>(requestContentString);
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result { IntResult = r!.Parameter!.IntParameter }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Once);
    }
    
    [Fact]
    public async Task ResolveRequestedContext()
    {
        var requiredContextA = "ContextA";
        var requiredContextB = "ContextB";
        var requiredContextC = "ContextC:any:-:key";
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B", "C" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextC", "any:-:key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("C");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContext<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(requiredContextA) ?? false) ||
                    !(r.Context?.ContainsKey(requiredContextB) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextA, requiredContextB } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                if (!(r.Context?.ContainsKey(requiredContextC) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextC } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Parameter!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextC", "any:-:key", It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(3));
    }

    [Fact]
    public async Task ResolveMultipleRequestedContexts()
    {
        var requiredContextA = "ContextA";
        var requiredContextB = "ContextB";
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContext<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(requiredContextA) ?? false) ||
                    !(r.Context?.ContainsKey(requiredContextB) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextA, requiredContextB } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Parameter!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveSingleRequestedContext()
    {
        var requiredContextA = "ContextA";
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContext<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(requiredContextA) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextA } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Parameter!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveChainedRequestedContexts()
    {
        var requiredContextA = "ContextA";
        var requiredContextB = "ContextB";
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "A", "B" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextB", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("B");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContext<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(requiredContextA) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextA } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                if (!(r.Context?.ContainsKey(requiredContextB) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextB } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }
                
                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Parameter!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextB", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(3));
    }

    [Theory]
    [InlineData("any:-:key")]
    [InlineData("simplekey")]
    [InlineData("simple-key")]
    [InlineData("https://snappet.org")]
    public async Task ResolveContextWithKey(string key)
    {
        var requiredContextC = $"ContextC:{key}";
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        var expectResult = new Result
            { IntResult = 66, Contexts = new[] { "C" } };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextC", key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("C");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                var requestContentString = await req.Content!.ReadAsStringAsync(default);
                var r = JsonConvert.DeserializeObject<RequestWithContext<Parameter>>(requestContentString);
                if (r == null)
                    throw new InvalidOperationException();

                if (!(r.Context?.ContainsKey(requiredContextC) ?? false))
                {
                    return new HttpResponseMessage
                    {
                        Content =
                            JsonContent.Create(new NeedContextResponse
                                { NeedContext = new[] { requiredContextC } }),
                        RequestMessage = req,
                        StatusCode = HttpStatusCode.BadRequest,
                    };
                }

                return new HttpResponseMessage
                {
                    Content = JsonContent.Create(new Result
                        { IntResult = r.Parameter!.IntParameter, 
                            Contexts = r.Context.Values.Select(v => v.ToString()).ToArray() }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.OK,
                };
            });
            
        

        Result result =
            await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        result.Should().BeEquivalentTo(expectResult);
        _contextProviderMock
            .Verify(p => p.GetContextAsync("ContextC", key, It.IsAny<CancellationToken>()), Times.Once);
        _httpMessageHandlerMock.Verify(HttpMessageHandlerSendAsync, Times.Exactly(2));
    }

    
    [Fact]
    public async Task ThrowHttpExceptionIfSubmittedContextIsRequestedAgain()
    {
        var requiredContextA = "ContextA";
        var postUri = new Uri("https://post-uri");
        var parameter = new Parameter { IntParameter = 66 };
        _contextProviderMock
            .Setup(p => p.GetContextAsync("ContextA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");

        _httpMessageHandlerMock
            .Setup(HttpMessageHandlerSendAsync)
            .ReturnsAsync( (HttpRequestMessage req, CancellationToken _) =>
            {
                return new HttpResponseMessage
                {
                    Content =
                        JsonContent.Create(new NeedContextResponse
                            { NeedContext = new[] { requiredContextA } }),
                    RequestMessage = req,
                    StatusCode = HttpStatusCode.BadRequest,
                };
            });
            
        Func<Task> act = 
            async () => await _sut.PostAsync(postUri, parameter, _contextProviderMock.Object, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    
}