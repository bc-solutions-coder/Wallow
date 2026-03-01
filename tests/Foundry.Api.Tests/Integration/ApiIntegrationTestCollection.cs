using Foundry.Tests.Common.Factories;

namespace Foundry.Api.Tests.Integration;

[CollectionDefinition(nameof(ApiIntegrationTestCollection))]
public class ApiIntegrationTestCollection : ICollectionFixture<FoundryApiFactory>
{
}
