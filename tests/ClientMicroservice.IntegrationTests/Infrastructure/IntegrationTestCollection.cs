namespace ClientMicroservice.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresContainerFixture> { }
