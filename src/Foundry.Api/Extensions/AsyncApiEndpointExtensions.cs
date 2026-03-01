using System.Reflection;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Api.Extensions;

internal static class AsyncApiEndpointExtensions
{
    public static WebApplication MapAsyncApiEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.", StringComparison.Ordinal) == true)
            .ToArray();

        EventFlowDiscovery discovery = new EventFlowDiscovery();
        EventFlowInfo[] flows = discovery.Discover(assemblies).ToArray();
        AsyncApiDocumentGenerator generator = new AsyncApiDocumentGenerator(flows);
        string mermaid = MermaidFlowGenerator.Generate(flows);

        app.MapGet("/asyncapi/v1.json", () => Results.Json(generator.GenerateDocument()))
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapGet("/asyncapi/v1/flows", () => Results.Text(mermaid, "text/plain"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapGet("/asyncapi", () => Results.Content(ViewerHtml, "text/html"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    private const string ViewerHtml =
        """
        <!DOCTYPE html>
        <html>
        <head>
            <title>Foundry AsyncAPI</title>
            <link rel="stylesheet" href="https://unpkg.com/@asyncapi/react-component@latest/styles/default.min.css">
        </head>
        <body>
            <div id="asyncapi"></div>
            <script src="https://unpkg.com/@asyncapi/react-component@latest/browser/standalone/index.js"></script>
            <script>
                fetch('/asyncapi/v1.json')
                    .then(r => r.json())
                    .then(schema => AsyncApiStandalone.render({ schema }, document.getElementById('asyncapi')));
            </script>
        </body>
        </html>
        """;
}
