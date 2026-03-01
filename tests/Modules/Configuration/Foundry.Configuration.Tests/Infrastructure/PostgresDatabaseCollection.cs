using Foundry.Tests.Common.Fixtures;

namespace Foundry.Configuration.Tests.Infrastructure;

[CollectionDefinition("PostgresDatabase")]
public class PostgresDatabaseCollection : ICollectionFixture<PostgresContainerFixture>;
