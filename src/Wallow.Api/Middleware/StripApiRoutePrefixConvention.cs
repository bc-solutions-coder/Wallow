using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Wallow.Api.Middleware;

/// <summary>
/// When PathBase is set to "/api", this convention strips the leading "api/" from controller
/// route templates so that routes don't double-prefix. Without this, PathBase="/api" strips
/// "/api" from the request path, but routes like "api/v1/identity/auth" still expect it,
/// causing every route to 404.
/// </summary>
internal sealed class StripApiRoutePrefixConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            foreach (SelectorModel selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel?.Template is { } template &&
                    template.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                {
                    selector.AttributeRouteModel.Template = template[4..];
                }
            }
        }
    }
}
