# AsyncAPI Event Catalog Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Auto-generate an AsyncAPI 3.0 specification from the codebase via reflection, serving it from API endpoints with an interactive visualization UI, so all 56 integration events, their schemas, producers, consumers, and saga flows are documented and always up-to-date.

**Architecture:** A reflection-based scanner discovers all `IIntegrationEvent` types and Wolverine handlers at startup, builds an in-memory AsyncAPI 3.0 document, and caches it. The spec is served as JSON from `/asyncapi/v1.json`, with an HTML viewer at `/asyncapi` using the AsyncAPI React component from CDN. A Mermaid diagram endpoint at `/asyncapi/v1/flows` renders event flow graphs. No external .NET packages needed -- uses `System.Text.Json.Nodes` throughout.

**Tech Stack:** .NET 10, System.Text.Json.Nodes, Wolverine handler conventions, AsyncAPI 3.0 spec, AsyncAPI React Component (CDN), Mermaid.js (CDN)

---

## Context

### Integration Event Conventions
- All integration events implement `IIntegrationEvent` (from `Foundry.Shared.Contracts`)
- Base record `IntegrationEvent` provides `EventId` (Guid) and `OccurredAt` (DateTime)
- Events live in `Foundry.Shared.Contracts.{ModuleName}.Events` -- the namespace encodes the source module
- Wolverine conventional routing creates a **fanout exchange** named after the event's full type name
- Consumer queues are auto-created per listener assembly

### Producer/Consumer Detection
- **Producer module**: Extracted from the event's namespace (`Foundry.Shared.Contracts.Billing.Events.InvoiceCreatedEvent` -> Billing)
- **Consumer module**: Found by scanning Wolverine handlers -- static `Handle`/`HandleAsync` methods whose first parameter matches the event type. The handler's namespace encodes the consuming module (`Foundry.Email.Application.EventHandlers` -> Email)
- **Sagas**: Classes inheriting `Wolverine.Saga` with `Start` methods (trigger event) and `Handle` methods (subsequent events)

### Existing Patterns
- OpenAPI served at `/openapi/v1.json` with Scalar UI at `/scalar/v1`
- Extension methods pattern: `services.AddXxx()` + `app.MapXxx()`
- All documentation endpoints are dev-only (`if (app.Environment.IsDevelopment())`)

---

## Task 1: JsonSchemaGenerator -- C# Types to JSON Schema

Converts C# record types (integration events) into JSON Schema objects compatible with AsyncAPI 3.0.

**Files:**
- Create: `src/Shared/Foundry.Shared.Infrastructure/AsyncApi/JsonSchemaGenerator.cs`
- Test: `tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/JsonSchemaGeneratorTests.cs`

**Step 1: Write failing tests for primitive type mapping and record schema generation**

```csharp
// tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/JsonSchemaGeneratorTests.cs
using System.Text.Json.Nodes;
using FluentAssertions;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Shared.Infrastructure.Tests.AsyncApi;

public class JsonSchemaGeneratorTests
{
    [Theory]
    [InlineData(typeof(string), "string", null)]
    [InlineData(typeof(int), "integer", null)]
    [InlineData(typeof(long), "integer", "int64")]
    [InlineData(typeof(decimal), "number", null)]
    [InlineData(typeof(double), "number", "double")]
    [InlineData(typeof(bool), "boolean", null)]
    [InlineData(typeof(Guid), "string", "uuid")]
    [InlineData(typeof(DateTime), "string", "date-time")]
    [InlineData(typeof(DateTimeOffset), "string", "date-time")]
    [InlineData(typeof(TimeSpan), "string", "duration")]
    public void GetPropertySchema_maps_primitive_types(Type type, string expectedType, string? expectedFormat)
    {
        JsonObject schema = JsonSchemaGenerator.GetPropertySchema(type);

        schema["type"]!.GetValue<string>().Should().Be(expectedType);
        if (expectedFormat is not null)
            schema["format"]!.GetValue<string>().Should().Be(expectedFormat);
        else
            schema.ContainsKey("format").Should().BeFalse();
    }

    [Fact]
    public void GetPropertySchema_nullable_returns_underlying_type()
    {
        JsonObject schema = JsonSchemaGenerator.GetPropertySchema(typeof(Guid?));

        schema["type"]!.GetValue<string>().Should().Be("string");
        schema["format"]!.GetValue<string>().Should().Be("uuid");
    }

    private sealed record TestEvent
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required decimal Amount { get; init; }
        public string? OptionalField { get; init; }
    }

    [Fact]
    public void GenerateSchema_creates_object_schema_with_properties_and_required()
    {
        JsonObject schema = JsonSchemaGenerator.GenerateSchema(typeof(TestEvent));

        schema["type"]!.GetValue<string>().Should().Be("object");

        JsonObject props = schema["properties"]!.AsObject();
        props.Should().ContainKey("id");
        props.Should().ContainKey("name");
        props.Should().ContainKey("amount");
        props.Should().ContainKey("optionalField");

        // id is Guid -> string/uuid
        props["id"]!["type"]!.GetValue<string>().Should().Be("string");
        props["id"]!["format"]!.GetValue<string>().Should().Be("uuid");

        // amount is decimal -> number
        props["amount"]!["type"]!.GetValue<string>().Should().Be("number");

        // required should include non-nullable properties only
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().Contain("id");
        required.Should().Contain("name");
        required.Should().Contain("amount");
        required.Should().NotContain("optionalField");
    }

    private sealed record EventWithCollection
    {
        public required IReadOnlyList<LineItem> Items { get; init; }
    }

    private sealed record LineItem
    {
        public required Guid ItemId { get; init; }
        public required int Quantity { get; init; }
    }

    [Fact]
    public void GenerateSchema_handles_collection_properties_with_nested_objects()
    {
        JsonObject schema = JsonSchemaGenerator.GenerateSchema(typeof(EventWithCollection));

        JsonObject items = schema["properties"]!["items"]!.AsObject();
        items["type"]!.GetValue<string>().Should().Be("array");

        JsonObject itemSchema = items["items"]!.AsObject();
        itemSchema["type"]!.GetValue<string>().Should().Be("object");
        itemSchema["properties"]!.AsObject().Should().ContainKey("itemId");
        itemSchema["properties"]!.AsObject().Should().ContainKey("quantity");
    }
}
```

**Step 2: Run tests -- verify they fail**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~JsonSchemaGenerator" -v minimal`
Expected: Build failure -- `JsonSchemaGenerator` does not exist.

**Step 3: Implement JsonSchemaGenerator**

```csharp
// src/Shared/Foundry.Shared.Infrastructure/AsyncApi/JsonSchemaGenerator.cs
using System.Reflection;
using System.Text.Json.Nodes;

namespace Foundry.Shared.Infrastructure.AsyncApi;

/// <summary>
/// Converts C# types to JSON Schema objects for AsyncAPI message payloads.
/// Property names are camelCased to match JSON serialization conventions.
/// </summary>
public static class JsonSchemaGenerator
{
    public static JsonObject GenerateSchema(Type type)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Concat(GetBaseProperties(type)))
        {
            string camelName = ToCamelCase(prop.Name);
            properties[camelName] = GetPropertySchema(prop.PropertyType);

            if (!IsNullable(prop))
                required.Add(camelName);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };
    }

    public static JsonObject GetPropertySchema(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return SimpleType("string");
        if (underlying == typeof(bool)) return SimpleType("boolean");
        if (underlying == typeof(int)) return SimpleType("integer");
        if (underlying == typeof(long)) return SimpleType("integer", "int64");
        if (underlying == typeof(decimal)) return SimpleType("number");
        if (underlying == typeof(double)) return SimpleType("number", "double");
        if (underlying == typeof(float)) return SimpleType("number", "float");
        if (underlying == typeof(Guid)) return SimpleType("string", "uuid");
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
            return SimpleType("string", "date-time");
        if (underlying == typeof(TimeSpan)) return SimpleType("string", "duration");

        if (IsCollection(underlying))
        {
            Type elementType = GetCollectionElementType(underlying);
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = GetPropertySchema(elementType)
            };
        }

        if (underlying.IsClass || underlying.IsValueType && !underlying.IsPrimitive)
            return GenerateSchema(underlying);

        return SimpleType("string");
    }

    private static JsonObject SimpleType(string type, string? format = null)
    {
        var obj = new JsonObject { ["type"] = type };
        if (format is not null) obj["format"] = format;
        return obj;
    }

    private static IEnumerable<PropertyInfo> GetBaseProperties(Type type)
    {
        // Include base class properties (e.g., IntegrationEvent.EventId, OccurredAt)
        Type? baseType = type.BaseType;
        while (baseType is not null && baseType != typeof(object))
        {
            foreach (PropertyInfo prop in baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                yield return prop;
            baseType = baseType.BaseType;
        }
    }

    private static bool IsNullable(PropertyInfo prop)
    {
        if (Nullable.GetUnderlyingType(prop.PropertyType) is not null) return true;

        var context = new NullabilityInfoContext();
        NullabilityInfo info = context.Create(prop);
        return info.ReadState == NullabilityState.Nullable;
    }

    private static bool IsCollection(Type type) =>
        type != typeof(string) &&
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    private static Type GetCollectionElementType(Type type)
    {
        Type? enumerable = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? typeof(object);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
```

**Step 4: Run tests -- verify they pass**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~JsonSchemaGenerator" -v minimal`
Expected: All tests PASS.

**Step 5: Commit**

```bash
git add src/Shared/Foundry.Shared.Infrastructure/AsyncApi/JsonSchemaGenerator.cs tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/JsonSchemaGeneratorTests.cs
git commit -m "feat(asyncapi): add JsonSchemaGenerator for C# type to JSON Schema conversion"
```

---

## Task 2: EventFlowDiscovery -- Scan Assemblies for Events, Producers, Consumers

Discovers all integration event types, their source modules (producers), consuming modules, and saga flows using reflection over Wolverine conventions.

**Files:**
- Create: `src/Shared/Foundry.Shared.Infrastructure/AsyncApi/EventFlowDiscovery.cs`
- Test: `tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/EventFlowDiscoveryTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/EventFlowDiscoveryTests.cs
using FluentAssertions;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Shared.Infrastructure.Tests.AsyncApi;

public class EventFlowDiscoveryTests
{
    [Fact]
    public void Discover_finds_all_integration_events_from_contracts_assembly()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true)
            .ToList();

        EventFlowDiscovery discovery = new();
        List<EventFlowInfo> flows = discovery.Discover(assemblies);

        // We know there are 50+ integration events
        flows.Should().HaveCountGreaterThan(40);

        // Every flow should have a valid event type and source module
        flows.Should().AllSatisfy(f =>
        {
            f.EventType.Should().NotBeNull();
            f.SourceModule.Should().NotBeNullOrWhiteSpace();
            f.EventTypeName.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void Discover_extracts_source_module_from_namespace()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true)
            .ToList();

        EventFlowDiscovery discovery = new();
        List<EventFlowInfo> flows = discovery.Discover(assemblies);

        EventFlowInfo? userRegistered = flows.FirstOrDefault(
            f => f.EventTypeName == "UserRegisteredEvent");

        userRegistered.Should().NotBeNull();
        userRegistered!.SourceModule.Should().Be("Identity");
    }

    [Fact]
    public void Discover_finds_consumers_from_wolverine_handlers()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true)
            .ToList();

        EventFlowDiscovery discovery = new();
        List<EventFlowInfo> flows = discovery.Discover(assemblies);

        EventFlowInfo? userRegistered = flows.FirstOrDefault(
            f => f.EventTypeName == "UserRegisteredEvent");

        userRegistered.Should().NotBeNull();
        // UserRegisteredEvent is consumed by Email, Notifications, Activity, Onboarding
        userRegistered!.ConsumerModules.Should().Contain("Email");
        userRegistered!.ConsumerModules.Should().Contain("Notifications");
    }

    [Fact]
    public void Discover_detects_saga_consumers()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true)
            .ToList();

        EventFlowDiscovery discovery = new();
        List<EventFlowInfo> flows = discovery.Discover(assemblies);

        EventFlowInfo? orderPlaced = flows.FirstOrDefault(
            f => f.EventTypeName == "OrderPlaced");

        orderPlaced.Should().NotBeNull();
        // OrderPlaced triggers OrderInventorySaga
        orderPlaced!.ConsumerModules.Should().Contain("Inventory");
        orderPlaced!.SagaTrigger.Should().BeTrue();
    }
}
```

**Step 2: Run tests -- verify they fail**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~EventFlowDiscovery" -v minimal`
Expected: Build failure -- types don't exist.

**Step 3: Implement EventFlowDiscovery**

```csharp
// src/Shared/Foundry.Shared.Infrastructure/AsyncApi/EventFlowDiscovery.cs
using System.Reflection;
using Foundry.Shared.Contracts;

namespace Foundry.Shared.Infrastructure.AsyncApi;

/// <summary>
/// Result of discovering one integration event's flow through the system.
/// </summary>
public sealed record EventFlowInfo(
    Type EventType,
    string EventTypeName,
    string SourceModule,
    string ExchangeName,
    List<ConsumerInfo> Consumers,
    bool SagaTrigger)
{
    public IReadOnlyList<string> ConsumerModules =>
        Consumers.Select(c => c.Module).Distinct().ToList();
}

public sealed record ConsumerInfo(
    string Module,
    string HandlerTypeName,
    string HandlerMethodName,
    bool IsSaga);

/// <summary>
/// Scans assemblies to discover integration events, their source modules,
/// and consuming handlers using Wolverine naming conventions.
/// </summary>
public sealed class EventFlowDiscovery
{
    private const string ContractsNamespacePrefix = "Foundry.Shared.Contracts.";
    private const string ModuleNamespacePrefix = "Foundry.";

    public List<EventFlowInfo> Discover(IEnumerable<Assembly> assemblies)
    {
        List<Assembly> assemblyList = assemblies.ToList();

        // 1. Find all IIntegrationEvent implementations
        List<Type> eventTypes = assemblyList
            .SelectMany(SafeGetTypes)
            .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t)
                && !t.IsAbstract && !t.IsInterface)
            .ToList();

        // 2. Find all Wolverine handlers (static Handle/HandleAsync methods)
        List<(MethodInfo Method, Type DeclaringType)> handlers = assemblyList
            .SelectMany(SafeGetTypes)
            .Where(t => t.IsClass)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name is "Handle" or "HandleAsync")
                .Select(m => (Method: m, DeclaringType: t)))
            .ToList();

        // 3. Find saga types (classes inheriting Saga with Start methods)
        List<Type> sagaTypes = assemblyList
            .SelectMany(SafeGetTypes)
            .Where(IsSagaType)
            .ToList();

        // 4. Build flows
        var flows = new List<EventFlowInfo>();
        foreach (Type eventType in eventTypes)
        {
            string sourceModule = ExtractModuleFromContractsNamespace(eventType);
            string exchangeName = eventType.FullName ?? eventType.Name;

            // Find handlers consuming this event
            var consumers = new List<ConsumerInfo>();

            // Regular handlers
            foreach (var (method, declaringType) in handlers)
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType == eventType)
                {
                    string module = ExtractModuleFromHandlerNamespace(declaringType);
                    consumers.Add(new ConsumerInfo(
                        module,
                        declaringType.Name,
                        method.Name,
                        IsSaga: false));
                }
            }

            // Saga handlers (instance methods on saga types)
            bool isSagaTrigger = false;
            foreach (Type sagaType in sagaTypes)
            {
                // Check Start method
                foreach (MethodInfo startMethod in sagaType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "Start"))
                {
                    ParameterInfo[] parameters = startMethod.GetParameters();
                    if (parameters.Length > 0 && parameters[0].ParameterType == eventType)
                    {
                        string module = ExtractModuleFromHandlerNamespace(sagaType);
                        consumers.Add(new ConsumerInfo(module, sagaType.Name, "Start", IsSaga: true));
                        isSagaTrigger = true;
                    }
                }

                // Check instance Handle methods
                foreach (MethodInfo handleMethod in sagaType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name is "Handle" or "HandleAsync"))
                {
                    ParameterInfo[] parameters = handleMethod.GetParameters();
                    if (parameters.Length > 0 && parameters[0].ParameterType == eventType)
                    {
                        string module = ExtractModuleFromHandlerNamespace(sagaType);
                        consumers.Add(new ConsumerInfo(module, sagaType.Name, handleMethod.Name, IsSaga: true));
                    }
                }
            }

            flows.Add(new EventFlowInfo(
                eventType,
                eventType.Name,
                sourceModule,
                exchangeName,
                consumers,
                isSagaTrigger));
        }

        return flows.OrderBy(f => f.SourceModule).ThenBy(f => f.EventTypeName).ToList();
    }

    private static string ExtractModuleFromContractsNamespace(Type type)
    {
        // Foundry.Shared.Contracts.{Module}.Events.EventName -> Module
        // Foundry.Shared.Contracts.Sales.OrderPlaced -> Sales
        string? ns = type.Namespace;
        if (ns is null || !ns.StartsWith(ContractsNamespacePrefix))
            return "Unknown";

        string remainder = ns[ContractsNamespacePrefix.Length..];
        int dotIndex = remainder.IndexOf('.');
        return dotIndex > 0 ? remainder[..dotIndex] : remainder;
    }

    private static string ExtractModuleFromHandlerNamespace(Type type)
    {
        // Foundry.{Module}.Application.EventHandlers.Handler -> Module
        // Foundry.{Module}.Infrastructure.Consumers.Consumer -> Module
        string? ns = type.Namespace;
        if (ns is null || !ns.StartsWith(ModuleNamespacePrefix))
            return "Unknown";

        string remainder = ns[ModuleNamespacePrefix.Length..];

        // Skip "Shared." prefix if present
        if (remainder.StartsWith("Shared."))
            return "Shared";

        int dotIndex = remainder.IndexOf('.');
        return dotIndex > 0 ? remainder[..dotIndex] : remainder;
    }

    private static bool IsSagaType(Type type) =>
        !type.IsAbstract && type.IsClass &&
        GetBaseTypes(type).Any(bt => bt.Name == "Saga");

    private static IEnumerable<Type> GetBaseTypes(Type type)
    {
        Type? current = type.BaseType;
        while (current is not null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
```

**Step 4: Run tests -- verify they pass**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~EventFlowDiscovery" -v minimal`
Expected: All tests PASS.

Note: These tests require the Foundry assemblies to be loaded. Since the test project references `Foundry.Shared.Infrastructure` which transitively references the contracts, the event types should be available. If not, add a project reference to `Foundry.Shared.Contracts` in the test project. The handler types require references to the module Application assemblies -- if they aren't loaded, the consumer detection tests may need to be adjusted to integration tests that run from the Api project context.

**Step 5: Commit**

```bash
git add src/Shared/Foundry.Shared.Infrastructure/AsyncApi/EventFlowDiscovery.cs tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/EventFlowDiscoveryTests.cs
git commit -m "feat(asyncapi): add EventFlowDiscovery for assembly-based event/handler scanning"
```

---

## Task 3: AsyncApiDocumentGenerator -- Build the Full Spec

Combines JsonSchemaGenerator and EventFlowDiscovery to produce a complete AsyncAPI 3.0 document as a `JsonObject`.

**Files:**
- Create: `src/Shared/Foundry.Shared.Infrastructure/AsyncApi/AsyncApiDocumentGenerator.cs`
- Test: `tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/AsyncApiDocumentGeneratorTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/AsyncApiDocumentGeneratorTests.cs
using System.Text.Json.Nodes;
using FluentAssertions;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Shared.Infrastructure.Tests.AsyncApi;

public class AsyncApiDocumentGeneratorTests
{
    [Fact]
    public void Generate_produces_valid_asyncapi_3_document_structure()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        // Root level
        doc["asyncapi"]!.GetValue<string>().Should().Be("3.0.0");
        doc["info"].Should().NotBeNull();
        doc["servers"].Should().NotBeNull();
        doc["channels"].Should().NotBeNull();
        doc["operations"].Should().NotBeNull();
        doc["components"].Should().NotBeNull();

        // Info
        doc["info"]!["title"]!.GetValue<string>().Should().Be("Foundry Event-Driven API");
        doc["info"]!["version"].Should().NotBeNull();
    }

    [Fact]
    public void Generate_includes_channels_for_all_integration_events()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        JsonObject channels = doc["channels"]!.AsObject();
        channels.Count.Should().BeGreaterThan(40);
    }

    [Fact]
    public void Generate_includes_message_schemas_in_components()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        JsonObject messages = doc["components"]!["messages"]!.AsObject();
        JsonObject schemas = doc["components"]!["schemas"]!.AsObject();

        messages.Count.Should().BeGreaterThan(40);
        schemas.Count.Should().BeGreaterThan(40);

        // Verify a known message has correct structure
        messages.Should().ContainKey("UserRegisteredEvent");
        JsonObject msg = messages["UserRegisteredEvent"]!.AsObject();
        msg["name"]!.GetValue<string>().Should().Be("UserRegisteredEvent");
        msg["payload"].Should().NotBeNull();
    }

    [Fact]
    public void Generate_includes_operations_for_producers_and_consumers()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        JsonObject operations = doc["operations"]!.AsObject();

        // Should have at least one send operation (producer) per event
        operations.Count.Should().BeGreaterThan(40);

        // Verify producer operation
        bool hasSendOp = operations.Any(kvp =>
            kvp.Value!["action"]!.GetValue<string>() == "send");
        hasSendOp.Should().BeTrue();

        // Verify consumer operation
        bool hasReceiveOp = operations.Any(kvp =>
            kvp.Value!["action"]!.GetValue<string>() == "receive");
        hasReceiveOp.Should().BeTrue();
    }

    [Fact]
    public void Generate_tags_operations_by_module()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        JsonObject operations = doc["operations"]!.AsObject();

        // Find any operation and verify it has tags
        JsonObject firstOp = operations.First().Value!.AsObject();
        firstOp["tags"].Should().NotBeNull();
        firstOp["tags"]!.AsArray().Should().NotBeEmpty();
    }
}
```

**Step 2: Run tests -- verify they fail**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~AsyncApiDocumentGenerator" -v minimal`
Expected: Build failure.

**Step 3: Implement AsyncApiDocumentGenerator**

```csharp
// src/Shared/Foundry.Shared.Infrastructure/AsyncApi/AsyncApiDocumentGenerator.cs
using System.Reflection;
using System.Text.Json.Nodes;

namespace Foundry.Shared.Infrastructure.AsyncApi;

/// <summary>
/// Generates a complete AsyncAPI 3.0 document by scanning assemblies
/// for integration events and Wolverine handlers.
/// </summary>
public sealed class AsyncApiDocumentGenerator
{
    private readonly string _title;
    private readonly string _version;
    private readonly string _rabbitMqHost;

    public AsyncApiDocumentGenerator(
        string title = "Foundry Event-Driven API",
        string version = "1.0.0",
        string rabbitMqHost = "localhost:5672")
    {
        _title = title;
        _version = version;
        _rabbitMqHost = rabbitMqHost;
    }

    public JsonObject Generate()
    {
        IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true);

        return Generate(assemblies);
    }

    public JsonObject Generate(IEnumerable<Assembly> assemblies)
    {
        EventFlowDiscovery discovery = new();
        List<EventFlowInfo> flows = discovery.Discover(assemblies);

        var channels = new JsonObject();
        var operations = new JsonObject();
        var messages = new JsonObject();
        var schemas = new JsonObject();

        foreach (EventFlowInfo flow in flows)
        {
            string channelKey = BuildChannelKey(flow);
            string messageKey = flow.EventTypeName;

            // Schema
            schemas[messageKey] = JsonSchemaGenerator.GenerateSchema(flow.EventType);

            // Message
            messages[messageKey] = new JsonObject
            {
                ["name"] = flow.EventTypeName,
                ["title"] = HumanizeEventName(flow.EventTypeName),
                ["summary"] = $"Published by {flow.SourceModule} module",
                ["contentType"] = "application/json",
                ["payload"] = new JsonObject
                {
                    ["$ref"] = $"#/components/schemas/{messageKey}"
                }
            };

            // Channel
            channels[channelKey] = new JsonObject
            {
                ["address"] = flow.ExchangeName,
                ["messages"] = new JsonObject
                {
                    [messageKey] = new JsonObject
                    {
                        ["$ref"] = $"#/components/messages/{messageKey}"
                    }
                },
                ["description"] = $"Exchange for {HumanizeEventName(flow.EventTypeName)} from {flow.SourceModule}",
                ["bindings"] = new JsonObject
                {
                    ["amqp"] = new JsonObject
                    {
                        ["is"] = "routingKey",
                        ["exchange"] = new JsonObject
                        {
                            ["name"] = flow.ExchangeName,
                            ["type"] = "fanout",
                            ["durable"] = true,
                            ["autoDelete"] = false
                        },
                        ["bindingVersion"] = "0.3.0"
                    }
                }
            };

            // Producer operation (send)
            string sendOpKey = $"{flow.SourceModule.ToLowerInvariant()}.publish{StripEventSuffix(flow.EventTypeName)}";
            operations[sendOpKey] = new JsonObject
            {
                ["action"] = "send",
                ["channel"] = new JsonObject
                {
                    ["$ref"] = $"#/channels/{channelKey}"
                },
                ["summary"] = $"{flow.SourceModule} publishes {HumanizeEventName(flow.EventTypeName)}",
                ["tags"] = new JsonArray
                {
                    new JsonObject { ["name"] = flow.SourceModule.ToLowerInvariant() }
                },
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["$ref"] = $"#/channels/{channelKey}/messages/{messageKey}"
                    }
                }
            };

            // Consumer operations (receive)
            foreach (ConsumerInfo consumer in flow.Consumers)
            {
                string receiveOpKey = $"{consumer.Module.ToLowerInvariant()}.on{StripEventSuffix(flow.EventTypeName)}";

                // Handle duplicate keys (multiple handlers in same module)
                if (operations.ContainsKey(receiveOpKey))
                    receiveOpKey += $".{consumer.HandlerTypeName.ToLowerInvariant()}";

                string sagaNote = consumer.IsSaga ? " (saga)" : "";
                operations[receiveOpKey] = new JsonObject
                {
                    ["action"] = "receive",
                    ["channel"] = new JsonObject
                    {
                        ["$ref"] = $"#/channels/{channelKey}"
                    },
                    ["summary"] = $"{consumer.Module} handles {HumanizeEventName(flow.EventTypeName)} via {consumer.HandlerTypeName}{sagaNote}",
                    ["tags"] = new JsonArray
                    {
                        new JsonObject { ["name"] = consumer.Module.ToLowerInvariant() },
                        consumer.IsSaga
                            ? new JsonObject { ["name"] = "saga" }
                            : null
                    }.Where(n => n is not null).Select(n => (JsonNode)n!).ToArray() is var tagNodes
                        ? new JsonArray(tagNodes)
                        : new JsonArray(),
                    ["messages"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["$ref"] = $"#/channels/{channelKey}/messages/{messageKey}"
                        }
                    }
                };
            }
        }

        return new JsonObject
        {
            ["asyncapi"] = "3.0.0",
            ["info"] = new JsonObject
            {
                ["title"] = _title,
                ["version"] = _version,
                ["description"] = "Auto-generated AsyncAPI specification documenting all integration events in the Foundry modular monolith. Events flow through RabbitMQ using Wolverine's conventional routing."
            },
            ["servers"] = new JsonObject
            {
                ["rabbitmq"] = new JsonObject
                {
                    ["host"] = _rabbitMqHost,
                    ["protocol"] = "amqp",
                    ["description"] = "RabbitMQ message broker"
                }
            },
            ["channels"] = channels,
            ["operations"] = operations,
            ["components"] = new JsonObject
            {
                ["messages"] = messages,
                ["schemas"] = schemas
            }
        };
    }

    private static string BuildChannelKey(EventFlowInfo flow) =>
        $"{flow.SourceModule.ToLowerInvariant()}.{ToCamelCase(StripEventSuffix(flow.EventTypeName))}";

    private static string StripEventSuffix(string name) =>
        name.EndsWith("Event") ? name[..^5] : name;

    private static string HumanizeEventName(string name)
    {
        string stripped = StripEventSuffix(name);
        // Insert spaces before uppercase letters
        return string.Concat(stripped.Select((c, i) =>
            i > 0 && char.IsUpper(c) && !char.IsUpper(stripped[i - 1]) ? $" {c}" : $"{c}"));
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
```

**Step 4: Run tests -- verify they pass**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~AsyncApiDocumentGenerator" -v minimal`
Expected: All tests PASS.

**Step 5: Commit**

```bash
git add src/Shared/Foundry.Shared.Infrastructure/AsyncApi/AsyncApiDocumentGenerator.cs tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/AsyncApiDocumentGeneratorTests.cs
git commit -m "feat(asyncapi): add AsyncApiDocumentGenerator for full spec generation"
```

---

## Task 4: API Endpoints -- Serve the AsyncAPI Spec

Add endpoints to serve the generated AsyncAPI spec and an interactive visualization UI.

**Files:**
- Create: `src/Foundry.Api/Extensions/AsyncApiEndpointExtensions.cs`
- Modify: `src/Foundry.Api/Program.cs` (add 2 lines to wire endpoints)
- Reference: `src/Foundry.Api/Extensions/ServiceCollectionExtensions.cs` (follow existing pattern)

**Step 1: Create the endpoint extension**

```csharp
// src/Foundry.Api/Extensions/AsyncApiEndpointExtensions.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Api.Extensions;

public static class AsyncApiEndpointExtensions
{
    private static JsonObject? _cachedDocument;
    private static string? _cachedJson;
    private static string? _cachedMermaid;

    public static WebApplication MapAsyncApi(this WebApplication app)
    {
        // JSON spec endpoint
        app.MapGet("/asyncapi/v1.json", () =>
        {
            if (_cachedJson is null)
            {
                AsyncApiDocumentGenerator generator = new();
                _cachedDocument = generator.Generate();
                _cachedJson = _cachedDocument.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            return Results.Content(_cachedJson, "application/json");
        })
        .ExcludeFromDescription()
        .AllowAnonymous();

        // Mermaid flow diagram endpoint
        app.MapGet("/asyncapi/v1/flows", () =>
        {
            if (_cachedDocument is null)
            {
                AsyncApiDocumentGenerator generator = new();
                _cachedDocument = generator.Generate();
            }

            _cachedMermaid ??= MermaidFlowGenerator.Generate(_cachedDocument);
            return Results.Content(_cachedMermaid, "text/plain");
        })
        .ExcludeFromDescription()
        .AllowAnonymous();

        // Interactive HTML viewer
        app.MapGet("/asyncapi", () => Results.Content(AsyncApiViewerHtml, "text/html"))
            .ExcludeFromDescription()
            .AllowAnonymous();

        return app;
    }

    private const string AsyncApiViewerHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Foundry AsyncAPI</title>
            <link rel="stylesheet" href="https://unpkg.com/@asyncapi/react-component@2/styles/default.min.css">
            <style>
                body { margin: 0; padding: 0; font-family: system-ui, -apple-system, sans-serif; }
                #asyncapi { max-width: 1400px; margin: 0 auto; padding: 20px; }
                .header { background: #1a1a2e; color: white; padding: 16px 20px; margin-bottom: 20px; }
                .header h1 { margin: 0; font-size: 1.4em; }
                .header p { margin: 4px 0 0; opacity: 0.8; font-size: 0.9em; }
                .nav { display: flex; gap: 16px; margin-top: 8px; }
                .nav a { color: #7c83ff; text-decoration: none; font-size: 0.85em; }
                .nav a:hover { text-decoration: underline; }
            </style>
        </head>
        <body>
            <div class="header">
                <h1>Foundry Event-Driven API</h1>
                <p>AsyncAPI 3.0 specification — auto-generated from codebase</p>
                <div class="nav">
                    <a href="/asyncapi/v1.json" target="_blank">Raw JSON</a>
                    <a href="/asyncapi/v1/flows" target="_blank">Mermaid Flow Diagram</a>
                    <a href="/scalar/v1">REST API Docs</a>
                </div>
            </div>
            <div id="asyncapi"></div>
            <script src="https://unpkg.com/@asyncapi/react-component@2/browser/standalone/index.js"></script>
            <script>
                AsyncApiStandalone.render({
                    schema: { url: '/asyncapi/v1.json' },
                    config: {
                        show: { sidebar: true, errors: false }
                    }
                }, document.getElementById('asyncapi'));
            </script>
        </body>
        </html>
        """;
}
```

**Step 2: Wire into Program.cs**

In `src/Foundry.Api/Program.cs`, find the dev-only documentation block (around the `MapOpenApi` / `MapScalarApiReference` calls) and add:

```csharp
// Inside: if (app.Environment.IsDevelopment()) { ... }
// After: app.MapScalarApiReference(...)
app.MapAsyncApi();
```

Add the using directive at the top of Program.cs if not already present. The `AsyncApiEndpointExtensions` class is in `Foundry.Api.Extensions` which should already be in scope.

**Step 3: Run the API and verify endpoints**

Run: `dotnet run --project src/Foundry.Api`
Then test:
- `curl http://localhost:5000/asyncapi/v1.json | head -20` -- should return AsyncAPI 3.0 JSON
- `curl http://localhost:5000/asyncapi` -- should return HTML viewer
- `curl http://localhost:5000/asyncapi/v1/flows` -- should return Mermaid diagram
- Open `http://localhost:5000/asyncapi` in browser -- should render interactive docs

**Step 4: Commit**

```bash
git add src/Foundry.Api/Extensions/AsyncApiEndpointExtensions.cs src/Foundry.Api/Program.cs
git commit -m "feat(asyncapi): add API endpoints for spec, viewer, and flow diagram"
```

---

## Task 5: MermaidFlowGenerator -- Visual Event Flow Diagrams

Generates Mermaid flowchart syntax from the AsyncAPI document, showing module-to-module event flows.

**Files:**
- Create: `src/Shared/Foundry.Shared.Infrastructure/AsyncApi/MermaidFlowGenerator.cs`
- Test: `tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/MermaidFlowGeneratorTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/MermaidFlowGeneratorTests.cs
using System.Text.Json.Nodes;
using FluentAssertions;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Shared.Infrastructure.Tests.AsyncApi;

public class MermaidFlowGeneratorTests
{
    [Fact]
    public void GenerateFromFlows_produces_valid_mermaid_flowchart()
    {
        var flows = new List<EventFlowInfo>
        {
            new(
                EventType: typeof(string), // placeholder
                EventTypeName: "UserRegisteredEvent",
                SourceModule: "Identity",
                ExchangeName: "Foundry.Shared.Contracts.Identity.Events.UserRegisteredEvent",
                Consumers: new List<ConsumerInfo>
                {
                    new("Email", "UserRegisteredEventHandler", "HandleAsync", false),
                    new("Notifications", "UserRegisteredEventHandler", "HandleAsync", false),
                },
                SagaTrigger: false)
        };

        string mermaid = MermaidFlowGenerator.GenerateFromFlows(flows);

        mermaid.Should().StartWith("flowchart LR");
        mermaid.Should().Contain("Identity");
        mermaid.Should().Contain("UserRegistered");
        mermaid.Should().Contain("Email");
        mermaid.Should().Contain("Notifications");
    }

    [Fact]
    public void GenerateFromFlows_marks_saga_connections_differently()
    {
        var flows = new List<EventFlowInfo>
        {
            new(
                EventType: typeof(string),
                EventTypeName: "OrderPlaced",
                SourceModule: "Sales",
                ExchangeName: "Foundry.Shared.Contracts.Sales.OrderPlaced",
                Consumers: new List<ConsumerInfo>
                {
                    new("Inventory", "OrderInventorySaga", "Start", true),
                },
                SagaTrigger: true)
        };

        string mermaid = MermaidFlowGenerator.GenerateFromFlows(flows);

        // Saga connections should use dotted lines
        mermaid.Should().Contain("-.->");
    }

    [Fact]
    public void Generate_from_full_document_produces_output()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        string mermaid = MermaidFlowGenerator.Generate(doc);

        mermaid.Should().StartWith("flowchart LR");
        mermaid.Should().Contain("Identity");
    }
}
```

**Step 2: Run tests -- verify they fail**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~MermaidFlowGenerator" -v minimal`
Expected: Build failure.

**Step 3: Implement MermaidFlowGenerator**

```csharp
// src/Shared/Foundry.Shared.Infrastructure/AsyncApi/MermaidFlowGenerator.cs
using System.Text;
using System.Text.Json.Nodes;

namespace Foundry.Shared.Infrastructure.AsyncApi;

/// <summary>
/// Generates Mermaid flowchart diagrams from event flow data,
/// showing module-to-module communication via integration events.
/// </summary>
public static class MermaidFlowGenerator
{
    public static string Generate(JsonObject asyncApiDocument)
    {
        // Re-discover flows from assemblies (the document doesn't store flow info directly)
        EventFlowDiscovery discovery = new();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Foundry.") == true);
        List<EventFlowInfo> flows = discovery.Discover(assemblies);
        return GenerateFromFlows(flows);
    }

    public static string GenerateFromFlows(IReadOnlyList<EventFlowInfo> flows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart LR");
        sb.AppendLine();

        // Collect unique modules for styling
        var producerModules = new HashSet<string>();
        var consumerModules = new HashSet<string>();
        var eventNodes = new HashSet<string>();

        foreach (EventFlowInfo flow in flows)
        {
            if (flow.Consumers.Count == 0) continue;

            string producerNodeId = SanitizeId(flow.SourceModule);
            string eventNodeId = SanitizeId(flow.EventTypeName);
            string eventLabel = StripEventSuffix(flow.EventTypeName);

            producerModules.Add(flow.SourceModule);
            eventNodes.Add(flow.EventTypeName);

            // Producer -> Event
            sb.AppendLine($"    {producerNodeId}[{flow.SourceModule}] --> {eventNodeId}[/\"{eventLabel}\"/]");

            // Event -> Consumers
            foreach (ConsumerInfo consumer in flow.Consumers)
            {
                string consumerNodeId = SanitizeId(consumer.Module);
                consumerModules.Add(consumer.Module);

                string arrow = consumer.IsSaga ? "-.->" : "-->";
                sb.AppendLine($"    {eventNodeId} {arrow} {consumerNodeId}[{consumer.Module}]");
            }

            sb.AppendLine();
        }

        // Styling
        sb.AppendLine("    %% Styles");
        foreach (string module in producerModules)
            sb.AppendLine($"    style {SanitizeId(module)} fill:#4a90d9,stroke:#2c5f8a,color:#fff");

        foreach (string module in consumerModules.Except(producerModules))
            sb.AppendLine($"    style {SanitizeId(module)} fill:#50c878,stroke:#2e7d4f,color:#fff");

        foreach (string evt in eventNodes)
            sb.AppendLine($"    style {SanitizeId(evt)} fill:#f5a623,stroke:#c7841a,color:#fff");

        return sb.ToString();
    }

    private static string StripEventSuffix(string name) =>
        name.EndsWith("Event") ? name[..^5] : name;

    private static string SanitizeId(string name) =>
        new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
}
```

**Step 4: Run tests -- verify they pass**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~MermaidFlowGenerator" -v minimal`
Expected: All tests PASS.

**Step 5: Commit**

```bash
git add src/Shared/Foundry.Shared.Infrastructure/AsyncApi/MermaidFlowGenerator.cs tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/MermaidFlowGeneratorTests.cs
git commit -m "feat(asyncapi): add MermaidFlowGenerator for visual event flow diagrams"
```

---

## Task 6: Integration Test -- Verify End-to-End

Verify the full pipeline works: assembly scanning -> document generation -> JSON serialization -> endpoint serving.

**Files:**
- Create: `tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/AsyncApiIntegrationTests.cs`

**Step 1: Write integration tests**

```csharp
// tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/AsyncApiIntegrationTests.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Foundry.Shared.Infrastructure.AsyncApi;

namespace Foundry.Shared.Infrastructure.Tests.AsyncApi;

public class AsyncApiIntegrationTests
{
    [Fact]
    public void Full_pipeline_generates_serializable_document()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        // Should serialize to valid JSON without throwing
        string json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        json.Should().NotBeNullOrWhiteSpace();

        // Should deserialize back
        JsonObject? parsed = JsonNode.Parse(json)?.AsObject();
        parsed.Should().NotBeNull();
        parsed!["asyncapi"]!.GetValue<string>().Should().Be("3.0.0");
    }

    [Fact]
    public void Known_event_flows_are_correctly_mapped()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        JsonObject operations = doc["operations"]!.AsObject();

        // Identity module should publish UserRegistered
        bool hasIdentityPublish = operations.Any(kvp =>
            kvp.Key.StartsWith("identity.") &&
            kvp.Value!["action"]!.GetValue<string>() == "send");
        hasIdentityPublish.Should().BeTrue("Identity module should have send operations");

        // Billing module should publish invoice events
        bool hasBillingPublish = operations.Any(kvp =>
            kvp.Key.StartsWith("billing.") &&
            kvp.Value!["action"]!.GetValue<string>() == "send");
        hasBillingPublish.Should().BeTrue("Billing module should have send operations");
    }

    [Fact]
    public void Schemas_have_base_event_properties()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        JsonObject schemas = doc["components"]!["schemas"]!.AsObject();

        // Every schema should have eventId and occurredAt from IntegrationEvent base
        foreach (var (name, schema) in schemas)
        {
            JsonObject props = schema!["properties"]!.AsObject();
            props.Should().ContainKey("eventId",
                $"schema {name} should have base property eventId");
            props.Should().ContainKey("occurredAt",
                $"schema {name} should have base property occurredAt");
        }
    }

    [Fact]
    public void Mermaid_diagram_is_non_empty_and_valid()
    {
        AsyncApiDocumentGenerator generator = new();
        JsonObject doc = generator.Generate();

        string mermaid = MermaidFlowGenerator.Generate(doc);

        mermaid.Should().StartWith("flowchart LR");
        mermaid.Length.Should().BeGreaterThan(100);
        mermaid.Should().Contain("-->");
    }
}
```

**Step 2: Run all AsyncApi tests**

Run: `dotnet test tests/Shared/Foundry.Shared.Infrastructure.Tests --filter "FullyQualifiedName~AsyncApi" -v minimal`
Expected: All tests PASS.

**Step 3: Run full test suite**

Run: `dotnet test`
Expected: All existing tests still pass (no regressions).

**Step 4: Commit**

```bash
git add tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/AsyncApiIntegrationTests.cs
git commit -m "test(asyncapi): add integration tests for full AsyncAPI pipeline"
```

---

## Summary

| Task | Files | What It Does |
|------|-------|-------------|
| 1 | `JsonSchemaGenerator.cs` + tests | C# types -> JSON Schema |
| 2 | `EventFlowDiscovery.cs` + tests | Reflection scanning for events & handlers |
| 3 | `AsyncApiDocumentGenerator.cs` + tests | Builds complete AsyncAPI 3.0 document |
| 4 | `AsyncApiEndpointExtensions.cs` + Program.cs | Serves spec, viewer, and Mermaid at `/asyncapi/*` |
| 5 | `MermaidFlowGenerator.cs` + tests | Visual flow diagrams |
| 6 | Integration tests | End-to-end verification |

### Endpoints After Implementation

| URL | Description |
|-----|-------------|
| `/asyncapi` | Interactive HTML viewer (AsyncAPI React component) |
| `/asyncapi/v1.json` | Raw AsyncAPI 3.0 JSON specification |
| `/asyncapi/v1/flows` | Mermaid flowchart of event flows |

### File Tree

```
src/Shared/Foundry.Shared.Infrastructure/AsyncApi/
    JsonSchemaGenerator.cs
    EventFlowDiscovery.cs
    AsyncApiDocumentGenerator.cs
    MermaidFlowGenerator.cs

src/Foundry.Api/Extensions/
    AsyncApiEndpointExtensions.cs

tests/Shared/Foundry.Shared.Infrastructure.Tests/AsyncApi/
    JsonSchemaGeneratorTests.cs
    EventFlowDiscoveryTests.cs
    AsyncApiDocumentGeneratorTests.cs
    MermaidFlowGeneratorTests.cs
    AsyncApiIntegrationTests.cs
```
