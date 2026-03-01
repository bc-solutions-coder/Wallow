using Foundry.Configuration.Application.Contracts.DTOs;
using Foundry.Configuration.Application.Queries;

namespace Foundry.Configuration.Tests.Application.Handlers;

public class GetSupportedEntityTypesHandlerTests
{
    private readonly GetSupportedEntityTypesHandler _handler;

    public GetSupportedEntityTypesHandlerTests()
    {
        _handler = new GetSupportedEntityTypesHandler();
    }

    [Fact]
    public async Task Handle_ReturnsEntityTypes()
    {
        GetSupportedEntityTypes query = new();

        IReadOnlyList<EntityTypeDto> result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsInvoiceEntityType()
    {
        GetSupportedEntityTypes query = new();

        IReadOnlyList<EntityTypeDto> result = await _handler.Handle(query, CancellationToken.None);

        result.Should().Contain(e => e.EntityType == "Invoice");
    }

    [Fact]
    public async Task Handle_EntityTypesHaveModuleAndDescription()
    {
        GetSupportedEntityTypes query = new();

        IReadOnlyList<EntityTypeDto> result = await _handler.Handle(query, CancellationToken.None);

        foreach (EntityTypeDto entityType in result)
        {
            entityType.Module.Should().NotBeNullOrWhiteSpace();
            entityType.Description.Should().NotBeNullOrWhiteSpace();
        }
    }
}
