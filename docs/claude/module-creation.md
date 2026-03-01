# Module Creation Guide

## Module Registration

Modules are registered using standard .NET extension methods. Each module provides `AddXxxModule()` and `InitializeXxxModuleAsync()` methods in its Infrastructure layer.

**Program.cs** calls into a central registry:
```csharp
// Service registration
FoundryModules.AddFoundryModules(builder.Services, builder.Configuration);

// Wolverine with automatic handler discovery
builder.Host.UseWolverine(opts =>
{
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true))
    {
        opts.Discovery.IncludeAssembly(assembly);
    }

    opts.UseRabbitMq(...)
        .AutoProvision()
        .UseConventionalRouting();
});

// Module initialization (runs migrations)
await FoundryModules.InitializeFoundryModulesAsync(app);
```

**FoundryModules.cs** (`src/Foundry.Api/FoundryModules.cs`) explicitly lists all modules:
```csharp
public static class FoundryModules
{
    public static IServiceCollection AddFoundryModules(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityModule(configuration);
        services.AddBillingModule(configuration);
        services.AddCommunicationsModule(configuration);
        services.AddStorageModule(configuration);
        services.AddConfigurationModule(configuration);
        services.AddFoundryPlugins(configuration);
        return services;
    }

    public static async Task InitializeFoundryModulesAsync(this WebApplication app)
    {
        await app.InitializeIdentityModuleAsync();
        await app.InitializeBillingModuleAsync();
        await app.InitializeCommunicationsModuleAsync();
        await app.InitializeStorageModuleAsync();
        await app.InitializeConfigurationModuleAsync();
        await app.InitializeFoundryPluginsAsync();
    }
}
```

## Creating a New Module

1. Create four projects:
   - `Foundry.{Module}.Domain`
   - `Foundry.{Module}.Application`
   - `Foundry.{Module}.Infrastructure`
   - `Foundry.{Module}.Api`

2. Create module extension methods in Infrastructure:

```csharp
// src/Modules/{Module}/Foundry.{Module}.Infrastructure/Extensions/{Module}ModuleExtensions.cs
public static class {Module}ModuleExtensions
{
    public static IServiceCollection Add{Module}Module(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Add{Module}Application();
        services.Add{Module}Infrastructure(configuration);
        return services;
    }

    public static async Task<WebApplication> Initialize{Module}ModuleAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<{Module}DbContext>();
        await db.Database.MigrateAsync();
        return app;
    }
}
```

3. Register the module in `src/Foundry.Api/FoundryModules.cs`:

```csharp
using Foundry.{Module}.Infrastructure.Extensions;

// In AddFoundryModules():
services.Add{Module}Module(configuration);

// In InitializeFoundryModulesAsync():
await app.Initialize{Module}ModuleAsync();
```

## Module Types

| Type | AddModule | InitializeModule | Notes |
|------|-----------|------------------|-------|
| **Standard** | Registers services, DbContext | Runs EF migrations | Most modules |
| **Stateless** | Registers services only | No-op or omit | No database |

## Handler Discovery

Wolverine automatically discovers handlers in all `Foundry.*` assemblies. No manual registration needed. Create handlers following Wolverine conventions:

```csharp
public static class CreateInvoiceHandler
{
    public static async Task<Result<InvoiceDto>> HandleAsync(
        CreateInvoiceCommand command, IInvoiceRepository repo, CancellationToken ct)
    {
        // Implementation
    }
}
```

## RabbitMQ Routing

Wolverine's `UseConventionalRouting()` automatically creates queues and exchanges. No manual `ConfigureMessaging` required.

## Shared Infrastructure Capabilities

Cross-cutting concerns in `Shared.Infrastructure` available to all modules:

- **Audit.NET interceptor** (`Shared.Infrastructure/Auditing/`) -- EF Core `SaveChangesInterceptor` that captures entity change audits. Registered globally; modules opt in via their DbContext.
- **IJobScheduler / Hangfire** (`Shared.Infrastructure/BackgroundJobs/`) -- `IJobScheduler` abstraction over Hangfire for enqueuing, scheduling, and recurring background jobs. Modules depend on the interface; Hangfire implementation is wired at the composition root.
- **Elsa 3 Workflows** (`Shared.Infrastructure/Workflows/`) -- Elsa 3 workflow engine integration with `WorkflowActivityBase` for defining custom activities. Modules define workflows without directly depending on Elsa internals.
