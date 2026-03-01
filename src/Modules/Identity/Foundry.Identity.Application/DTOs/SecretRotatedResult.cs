namespace Foundry.Identity.Application.DTOs;

public record SecretRotatedResult(
    string NewClientSecret,
    DateTime RotatedAt);
