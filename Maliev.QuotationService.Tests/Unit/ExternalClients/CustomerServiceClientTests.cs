using System.Net;
using System.Net.Http.Json;
using Maliev.QuotationService.Api.ExternalClients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.QuotationService.Tests.Unit.ExternalClients;

public sealed class CustomerServiceClientTests
{
    [Fact]
    public async Task GetCustomerByIdAsync_UsesVersionedCustomerServiceRoute()
    {
        var customerId = Guid.NewGuid();
        using var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = customerId,
                firstName = "Sarah",
                lastName = "Chen",
                name = "Sarah Chen",
                email = "sarah@axion.io",
                mobile = "+1 415 555 0142",
                companyName = "Axion Robotics"
            })
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://customer-service")
        };

        var logger = new Mock<ILogger<CustomerServiceClient>>();
        var client = new CustomerServiceClient(httpClient, logger.Object);

        var customer = await client.GetCustomerByIdAsync(customerId);

        Assert.NotNull(customer);
        Assert.Equal($"/customer/v1/customers/{customerId}", handler.RequestUri?.PathAndQuery);
        Assert.Equal(customerId, customer.Id);
        Assert.Equal("Sarah Chen", customer.Name);
        Assert.Equal("sarah@axion.io", customer.Email);
        Assert.Equal("+1 415 555 0142", customer.Mobile);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }
}
