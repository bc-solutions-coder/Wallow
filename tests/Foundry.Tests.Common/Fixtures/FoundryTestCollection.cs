using Foundry.Tests.Common.Factories;

namespace Foundry.Tests.Common.Fixtures;

[CollectionDefinition(nameof(FoundryTestCollection))]
public class FoundryTestCollection : ICollectionFixture<FoundryApiFactory>
{
}
