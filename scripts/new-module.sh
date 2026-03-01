#!/bin/bash
# new-module.sh - Scaffold a new Foundry module
# Usage: ./scripts/new-module.sh ModuleName

set -e

if [ -z "$1" ]; then
    echo "Usage: ./scripts/new-module.sh ModuleName"
    echo "Example: ./scripts/new-module.sh Inventory"
    exit 1
fi

MODULE_NAME="$1"
MODULE_LOWER=$(echo "$MODULE_NAME" | tr '[:upper:]' '[:lower:]')
BASE_PATH="src/Modules/${MODULE_NAME}"

echo "Creating module: ${MODULE_NAME}"
echo "Path: ${BASE_PATH}"
echo ""

# Create project directories
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Domain/Entities"
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Commands"
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Queries"
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Extensions"
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Persistence"
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Extensions"
mkdir -p "${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Controllers"

# Domain project
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Domain/Foundry.${MODULE_NAME}.Domain.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Foundry.Shared.Kernel\Foundry.Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
EOF

# Application project
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Foundry.${MODULE_NAME}.Application.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Foundry.${MODULE_NAME}.Domain\Foundry.${MODULE_NAME}.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\Foundry.Shared.Contracts\Foundry.Shared.Contracts.csproj" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="WolverineFx" />
  </ItemGroup>
</Project>
EOF

# Replace ${MODULE_NAME} in Application project
sed -i.bak "s/\${MODULE_NAME}/${MODULE_NAME}/g" "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Foundry.${MODULE_NAME}.Application.csproj"
rm "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Foundry.${MODULE_NAME}.Application.csproj.bak"

# Infrastructure project
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Foundry.${MODULE_NAME}.Infrastructure.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Foundry.${MODULE_NAME}.Application\Foundry.${MODULE_NAME}.Application.csproj" />
    <ProjectReference Include="..\..\..\Shared\Foundry.Shared.Kernel\Foundry.Shared.Kernel.csproj" />
    <ProjectReference Include="..\..\..\Shared\Foundry.Shared.Contracts\Foundry.Shared.Contracts.csproj" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>
</Project>
EOF

# Replace ${MODULE_NAME} in Infrastructure project
sed -i.bak "s/\${MODULE_NAME}/${MODULE_NAME}/g" "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Foundry.${MODULE_NAME}.Infrastructure.csproj"
rm "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Foundry.${MODULE_NAME}.Infrastructure.csproj.bak"

# Api project
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Foundry.${MODULE_NAME}.Api.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Foundry.${MODULE_NAME}.Application\Foundry.${MODULE_NAME}.Application.csproj" />
    <ProjectReference Include="..\Foundry.${MODULE_NAME}.Infrastructure\Foundry.${MODULE_NAME}.Infrastructure.csproj" />
    <ProjectReference Include="..\..\..\Shared\Foundry.Shared.Kernel\Foundry.Shared.Kernel.csproj" />
    <ProjectReference Include="..\..\..\Shared\Foundry.Shared.Contracts\Foundry.Shared.Contracts.csproj" />
    <PackageReference Include="WolverineFx" />
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
EOF

# Replace ${MODULE_NAME} in Api project
sed -i.bak "s/\${MODULE_NAME}/${MODULE_NAME}/g" "${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Foundry.${MODULE_NAME}.Api.csproj"
rm "${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Foundry.${MODULE_NAME}.Api.csproj.bak"

# Application Extensions
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Extensions/ApplicationExtensions.cs" << EOF
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.${MODULE_NAME}.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection Add${MODULE_NAME}Application(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        return services;
    }
}
EOF

# Infrastructure Extensions
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Extensions/InfrastructureExtensions.cs" << EOF
using Foundry.${MODULE_NAME}.Infrastructure.Persistence;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.${MODULE_NAME}.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection Add${MODULE_NAME}Infrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<${MODULE_NAME}DbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "${MODULE_LOWER}");
            });
            options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
        });

        // TODO: Register repositories
        // services.AddScoped<I${MODULE_NAME}Repository, ${MODULE_NAME}Repository>();

        return services;
    }
}
EOF

# DbContext
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Persistence/${MODULE_NAME}DbContext.cs" << EOF
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Foundry.${MODULE_NAME}.Infrastructure.Persistence;

public sealed class ${MODULE_NAME}DbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ${MODULE_NAME}DbContext(
        DbContextOptions<${MODULE_NAME}DbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("${MODULE_LOWER}");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(${MODULE_NAME}DbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantScoped.TenantId));
                var tenantId = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(_tenantContext),
                    nameof(ITenantContext.TenantId));
                var equals = System.Linq.Expressions.Expression.Equal(property, tenantId);
                var lambda = System.Linq.Expressions.Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
EOF

# DesignTimeTenantContext
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Persistence/DesignTimeTenantContext.cs" << EOF
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;

namespace Foundry.${MODULE_NAME}.Infrastructure.Persistence;

/// <summary>
/// Mock ITenantContext for design-time migrations.
/// Returns a placeholder TenantId that is never used at runtime.
/// </summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public TenantId TenantId => new(Guid.Parse("00000000-0000-0000-0000-000000000000"));
    public string TenantName => "design-time";
    public bool IsResolved => true;

    public void SetTenant(TenantId tenantId, string tenantName = "")
    {
        // No-op for design-time
    }

    public void Clear()
    {
        // No-op for design-time
    }
}
EOF

# Module Registration Class
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Api/${MODULE_NAME}Module.cs" << EOF
using System.Reflection;
using Foundry.${MODULE_NAME}.Application.Extensions;
using Foundry.${MODULE_NAME}.Infrastructure.Extensions;
using Foundry.${MODULE_NAME}.Infrastructure.Persistence;
using Foundry.Shared.Kernel.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Foundry.${MODULE_NAME}.Api;

/// <summary>
/// Module registration for ${MODULE_NAME}.
/// This class is auto-discovered by the module discovery mechanism.
/// </summary>
public sealed class ${MODULE_NAME}Module : IModuleRegistration
{
    public static string ModuleName => "${MODULE_NAME}";

    /// <summary>
    /// Assembly containing Wolverine command/query handlers.
    /// Return null if this module has no CQRS handlers.
    /// </summary>
    public static Assembly? HandlerAssembly => null;
    // TODO: Uncomment when you have handlers:
    // public static Assembly? HandlerAssembly =>
    //     typeof(Application.Commands.SomeCommand).Assembly;

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Add${MODULE_NAME}Application();
        services.Add${MODULE_NAME}Infrastructure(configuration);
    }

    public static async Task InitializeAsync(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<${MODULE_NAME}DbContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger<${MODULE_NAME}Module>();
            logger.LogWarning(ex, "${MODULE_NAME} module startup failed. Ensure PostgreSQL is running.");
        }
    }

    public static void ConfigureMessaging(WolverineOptions options)
    {
        // TODO: Configure RabbitMQ message routing
        // Example: Publishing events
        // options.PublishMessage<SomeEvent>().ToRabbitExchange("${MODULE_LOWER}-events");

        // Example: Listening to queue
        // options.ListenToRabbitQueue("${MODULE_LOWER}-inbox");
    }
}
EOF

# Sample Controller
cat > "${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Controllers/${MODULE_NAME}Controller.cs" << EOF
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Foundry.${MODULE_NAME}.Api.Controllers;

[ApiController]
[Route("api/${MODULE_LOWER}")]
[Authorize]
public class ${MODULE_NAME}Controller : ControllerBase
{
    private readonly IMessageBus _bus;

    public ${MODULE_NAME}Controller(IMessageBus bus)
    {
        _bus = bus;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { module = "${MODULE_NAME}", status = "ready" });
    }
}
EOF

echo "Module scaffolded successfully!"
echo ""
echo "Next steps:"
echo "1. Add the projects to the solution:"
echo "   dotnet sln add ${BASE_PATH}/Foundry.${MODULE_NAME}.Domain/Foundry.${MODULE_NAME}.Domain.csproj"
echo "   dotnet sln add ${BASE_PATH}/Foundry.${MODULE_NAME}.Application/Foundry.${MODULE_NAME}.Application.csproj"
echo "   dotnet sln add ${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure/Foundry.${MODULE_NAME}.Infrastructure.csproj"
echo "   dotnet sln add ${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Foundry.${MODULE_NAME}.Api.csproj"
echo ""
echo "2. Add reference from Foundry.Api to the new Api project:"
echo "   dotnet add src/Foundry.Api/Foundry.Api.csproj reference ${BASE_PATH}/Foundry.${MODULE_NAME}.Api/Foundry.${MODULE_NAME}.Api.csproj"
echo ""
echo "3. Create your domain entities in Domain/Entities/"
echo ""
echo "4. Create migrations:"
echo "   dotnet ef migrations add InitialCreate \\"
echo "       --project ${BASE_PATH}/Foundry.${MODULE_NAME}.Infrastructure \\"
echo "       --startup-project src/Foundry.Api \\"
echo "       --context ${MODULE_NAME}DbContext"
echo ""
echo "5. Add module to CleanArchitectureTests.cs and ModuleIsolationTests.cs"
