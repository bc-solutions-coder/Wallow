namespace Foundry.Identity.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}
