using Foundry.Identity.Domain.Identity;

namespace Foundry.Identity.Application.Commands.RotateServiceAccountSecret;

public sealed record RotateServiceAccountSecretCommand(ServiceAccountMetadataId Id);
