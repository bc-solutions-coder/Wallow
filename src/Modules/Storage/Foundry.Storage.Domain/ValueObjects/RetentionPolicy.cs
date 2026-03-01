using Foundry.Storage.Domain.Enums;

namespace Foundry.Storage.Domain.ValueObjects;

public sealed record RetentionPolicy(int Days, RetentionAction Action);
