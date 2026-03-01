using System.Threading.RateLimiting;
using Foundry.Api.Middleware;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using Serilog;

namespace Foundry.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Problem Details
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["api"] = "Foundry";
                context.ProblemDetails.Extensions["version"] = "1.0.0";
            };
        });

        // Global Exception Handler
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // OpenAPI documentation (Scalar)
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) => TransformDocumentInfo(document));
            options.AddDocumentTransformer((document, _, _) => TransformDocumentSecurity(document));
            options.AddOperationTransformer((operation, context, _) =>
                TransformOperationSecurity(operation, context));
        });

        // CORS
        string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (!environment.IsDevelopment() && allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins must be configured with at least one origin in non-Development environments.");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            // Named policy for development (explicit localhost origins for SignalR)
            options.AddPolicy("Development", policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Health checks - connection strings resolved lazily via factories
        // to support Testcontainers dynamic connection strings
        services.AddHealthChecks()
            .AddNpgSql(
                sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!,
                name: "postgresql", tags: ["db", "ready"])
            .AddRabbitMQ(sp =>
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                string rabbitHost = config["RabbitMQ:Host"]!;
                string rabbitUser = config["RabbitMQ:Username"]!;
                string rabbitPass = config["RabbitMQ:Password"]!;
                Uri rabbitUri = new Uri($"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672");
                ConnectionFactory factory = new RabbitMQ.Client.ConnectionFactory { Uri = rabbitUri };
                return factory.CreateConnectionAsync();
            }, name: "rabbitmq", tags: ["messaging", "ready"])
            .AddHangfire(options =>
            {
                options.MinimumAvailableServers = 1;
            }, name: "hangfire", tags: ["jobs", "ready"])
            .AddRedis(
                sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("Redis connection string not configured"),
                name: "redis",
                tags: ["infrastructure", "ready"]);

        return services;
    }

    public static IServiceCollection AddFoundryRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitDefaults.AuthPermitLimit,
                        Window = TimeSpan.FromMinutes(RateLimitDefaults.AuthWindowMinutes),
                        QueueLimit = 0
                    }));

            options.AddPolicy("upload", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitDefaults.UploadPermitLimit,
                        Window = TimeSpan.FromHours(RateLimitDefaults.UploadWindowHours),
                        QueueLimit = 0
                    }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitDefaults.GlobalPermitLimit,
                        Window = TimeSpan.FromHours(RateLimitDefaults.GlobalWindowHours),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        string serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Foundry";
        string? otlpGrpcEndpoint = configuration["OpenTelemetry:OtlpGrpcEndpoint"];

        if (!environment.IsDevelopment() && string.IsNullOrEmpty(otlpGrpcEndpoint))
        {
            throw new InvalidOperationException(
                "OpenTelemetry:OtlpGrpcEndpoint must be configured in non-Development environments. " +
                "Set the 'OpenTelemetry:OtlpGrpcEndpoint' configuration value.");
        }

        otlpGrpcEndpoint ??= "http://localhost:4317";
        Log.Information("OpenTelemetry OTLP endpoint: {OtlpEndpoint}", otlpGrpcEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceNamespace: "Foundry",
                    serviceVersion: typeof(ServiceCollectionExtensions).Assembly
                        .GetName().Version?.ToString() ?? "1.0.0")
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", environment.EnvironmentName),
                    new("service.instance.id", Environment.MachineName)
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = FilterTelemetryRequest;
                })
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Wolverine")
                .AddSource("Foundry")
                .AddSource("Foundry.*")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpGrpcEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Wolverine")
                .AddMeter("Foundry")
                .AddMeter("Foundry.*")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpGrpcEndpoint);
                }));

        return services;
    }

    internal static Task TransformDocumentInfo(OpenApiDocument document)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Foundry API",
            Version = "v1",
            Description = "A modular monolith API built with Clean Architecture, DDD, and CQRS",
            Contact = new OpenApiContact
            {
                Name = "Foundry"
            }
        };
        return Task.CompletedTask;
    }

    internal static Task TransformDocumentSecurity(OpenApiDocument document)
    {
        OpenApiComponents components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token"
        };

        OpenApiSecuritySchemeReference securitySchemeRef = new OpenApiSecuritySchemeReference("Bearer", document);
        document.Security = [new OpenApiSecurityRequirement { [securitySchemeRef] = [] }];

        return Task.CompletedTask;
    }

    internal static Task TransformOperationSecurity(
        OpenApiOperation operation,
        Microsoft.AspNetCore.OpenApi.OpenApiOperationTransformerContext context)
    {
        IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;
        bool hasAllowAnonymous = metadata
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
            .Any();

        if (hasAllowAnonymous)
        {
            operation.Security?.Clear();
        }

        return Task.CompletedTask;
    }

    internal static bool FilterTelemetryRequest(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/alive", StringComparison.OrdinalIgnoreCase);
    }
}
