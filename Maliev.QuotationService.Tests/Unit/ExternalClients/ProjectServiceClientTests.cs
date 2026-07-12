using System.Net;
using System.Net.Http.Json;
using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.QuotationService.Tests.Unit.ExternalClients;

public sealed class ProjectServiceClientTests
{
    [Fact]
    public async Task VerifyOwnershipAsync_OwnedProject_UsesExactRouteAndReturnsAuthoritativeNumber()
    {
        var projectId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        using var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = projectId,
                customerId,
                projectNumber = "PRJ-2026-0042",
                title = "Ignored by the ownership boundary"
            })
        }));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.VerifyOwnershipAsync(projectId, customerId);

        Assert.Equal(ProjectOwnershipStatus.Owned, result.Status);
        Assert.Equal("PRJ-2026-0042", result.ProjectNumber);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/project/v1/projects/{projectId:D}", handler.RequestUri?.PathAndQuery);
    }

    [Fact]
    public async Task VerifyOwnershipAsync_NotFound_ReturnsNotOwned()
    {
        using var handler = Responding(HttpStatusCode.NotFound);
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.NotOwned, result.Status);
    }

    [Fact]
    public async Task VerifyOwnershipAsync_DifferentCustomer_ReturnsNotOwned()
    {
        var projectId = Guid.NewGuid();
        using var handler = JsonResponse(new
        {
            id = projectId,
            customerId = Guid.NewGuid(),
            projectNumber = "PRJ-OTHER"
        });
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(projectId, Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.NotOwned, result.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task VerifyOwnershipAsync_DependencyOrIdentityFailure_ReturnsUnavailable(HttpStatusCode statusCode)
    {
        using var handler = Responding(statusCode);
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task VerifyOwnershipAsync_MismatchedResponseId_ReturnsUnavailable()
    {
        using var handler = JsonResponse(new
        {
            id = Guid.NewGuid(),
            customerId = Guid.NewGuid(),
            projectNumber = "PRJ-WRONG"
        });
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.Unavailable, result.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"id\":\"00000000-0000-0000-0000-000000000001\",\"customerId\":\"00000000-0000-0000-0000-000000000002\",\"projectNumber\":\"\"}")]
    public async Task VerifyOwnershipAsync_MalformedOrIncompleteBody_ReturnsUnavailable(string body)
    {
        using var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body)
        }));
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task VerifyOwnershipAsync_NetworkFailure_ReturnsUnavailable()
    {
        using var handler = new RecordingHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")));
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task VerifyOwnershipAsync_Timeout_ReturnsUnavailable()
    {
        using var handler = new RecordingHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout")));
        using var httpClient = CreateHttpClient(handler);

        var result = await CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectOwnershipStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task VerifyOwnershipAsync_CallerCancellation_RethrowsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var handler = new RecordingHandler((_, token) =>
            Task.FromCanceled<HttpResponseMessage>(token));
        using var httpClient = CreateHttpClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateClient(httpClient).VerifyOwnershipAsync(Guid.NewGuid(), Guid.NewGuid(), cancellation.Token));
    }

    private static ProjectServiceClient CreateClient(HttpClient httpClient)
    {
        var logger = new Mock<ILogger<ProjectServiceClient>>();
        return new ProjectServiceClient(httpClient, logger.Object);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://project-service")
    };

    private static RecordingHandler Responding(HttpStatusCode statusCode) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)));

    private static RecordingHandler JsonResponse(object value) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value)
        }));

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _response;

        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
        {
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        public HttpMethod? RequestMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestMethod = request.Method;
            return _response(request, cancellationToken);
        }
    }
}
