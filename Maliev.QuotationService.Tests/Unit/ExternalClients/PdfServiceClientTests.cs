using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.QuotationService.Tests.Unit.ExternalClients;

public sealed class PdfServiceClientTests
{
    [Fact]
    public async Task GeneratePdfAsync_UsesCurrentPdfGenerationRouteAndMapsStorageArtifact()
    {
        using var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://pdf.example.test")
        };
        var client = new PdfServiceClient(httpClient, NullLogger<PdfServiceClient>.Instance);
        var payload = new QuotationPdfPayload(
            "quotation-1-v1",
            new
            {
                quotationNumber = "quotation-1",
                versionNumber = 1,
                items = Array.Empty<object>()
            });

        var result = await client.GeneratePdfAsync(payload);

        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("/pdf/v1/generations/generate", handler.Request?.RequestUri?.AbsolutePath);
        Assert.NotNull(handler.Body);
        using var requestJson = JsonDocument.Parse(handler.Body);
        var root = requestJson.RootElement;
        Assert.Equal("Quotation", root.GetProperty("templateCode").GetString());
        Assert.Equal("quotation-1-v1", root.GetProperty("referenceId").GetString());
        Assert.Equal(0, root.GetProperty("documentType").GetInt32());
        Assert.Equal("quotation-1", root.GetProperty("data").GetProperty("quotationNumber").GetString());
        Assert.Equal("https://storage.example.test/quotation.pdf", result.StorageUrl);
        Assert.Equal("pdfs/quotation/quotation-1-v1/quotation.pdf", result.StoragePath);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    requestId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    storageUrl = "https://storage.example.test/quotation.pdf",
                    storagePath = "pdfs/quotation/quotation-1-v1/quotation.pdf"
                })
            };
        }
    }
}
