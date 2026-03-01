namespace Foundry.Storage.Application.Commands.DeleteBucket;

public sealed record DeleteBucketCommand(
    string Name,
    bool Force = false);
