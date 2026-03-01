using Foundry.Tests.Common.Fixtures;

namespace Foundry.Identity.Tests.Integration;

[CollectionDefinition("PostgresDatabase")]
public class PostgresDatabaseCollection : ICollectionFixture<PostgresContainerFixture>;
