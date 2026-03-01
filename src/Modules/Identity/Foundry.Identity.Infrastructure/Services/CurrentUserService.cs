using System.Security.Claims;
using Foundry.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Foundry.Identity.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            string? userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;

            if (userIdClaim is not null && Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }

            return null;
        }
    }
}
