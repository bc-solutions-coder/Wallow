using Foundry.Identity.Domain.Identity;

namespace Foundry.Identity.Application.Commands.RevokeServiceAccount;

public sealed record RevokeServiceAccountCommand(ServiceAccountMetadataId Id);
