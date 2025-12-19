using Xunit;

namespace Maliev.QuotationService.Tests.Fixtures;

/// <summary>
/// Collection definition to ensure all integration tests share the same test factory instance.
/// This prevents creating multiple PostgreSQL/Redis/RabbitMQ containers and improves test performance.
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
