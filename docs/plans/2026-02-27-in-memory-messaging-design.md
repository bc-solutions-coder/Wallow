# In-Memory Module Messaging

Replace RabbitMQ with Wolverine's built-in local (in-memory) queues for cross-module communication. The modular monolith runs in a single process; RabbitMQ adds infrastructure overhead without proportional benefit at this scale. The design preserves the ability to restore RabbitMQ when workload demands it.

## Motivation

RabbitMQ requires a running broker, Docker container, connection management, and network serialization for every message — all to deliver events between modules in the same process. Wolverine's local queues deliver the same events in-memory with identical handler semantics, zero infrastructure, and faster execution.

## Transport Switch

A configuration flag controls the transport. All environments default to in-memory.

**Configuration key:** `ModuleMessaging:Transport`
**Values:** `InMemory` (default), `RabbitMq`

### appsettings.json (base)

```json
{
  "ModuleMessaging": {
    "Transport": "InMemory"
  }
}
```

### appsettings.Production.json (when scaling requires RabbitMQ)

```json
{
  "ModuleMessaging": {
    "Transport": "RabbitMq"
  },
  "ConnectionStrings": {
    "RabbitMq": "amqp://user:pass@rabbitmq:5672"
  }
}
```

### Program.cs change

Replace the current connection-string-presence check:

```csharp
string? rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq");
if (!string.IsNullOrEmpty(rabbitMqConnection))
{
    // RabbitMQ setup...
}
```

With an explicit transport switch:

```csharp
var transport = builder.Configuration.GetValue<string>("ModuleMessaging:Transport") ?? "InMemory";

if (transport.Equals("RabbitMq", StringComparison.OrdinalIgnoreCase))
{
    string rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq")
        ?? throw new InvalidOperationException(
            "RabbitMq connection string is required when ModuleMessaging:Transport is 'RabbitMq'");

    var rabbitMq = opts.UseRabbitMq(new Uri(rabbitMqConnection))
        .AutoProvision()
        .UseConventionalRouting();

    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        rabbitMq.AutoPurgeOnStartup();
}
```

When the transport is `InMemory`, Wolverine routes all messages through local queues automatically. No additional configuration is needed.

## What stays the same

These components are transport-agnostic and require no changes:

- **PostgreSQL durable outbox** — `PersistMessagesWithPostgresql` and `UseDurableOutboxOnAllSendingEndpoints` work identically with local queues. Messages persist to Postgres; the app recovers unprocessed messages on restart.
- **Handler discovery** — Wolverine scans all `Foundry.*` assemblies regardless of transport.
- **Error handling** — Retry policies and dead-letter queue behavior apply to local queues.
- **FluentValidation middleware** — Validates commands before handlers execute.
- **Module tagging middleware** — Tags OpenTelemetry spans with `foundry.module`.
- **EF Core transaction integration** — Enlists messages in EF Core transactions.
- **All handlers and consumers** — Same `HandleAsync` signatures, same DI, same behavior.
- **Shared.Contracts events** — No changes to event definitions.
- **Domain-to-integration event bridge** — `bus.PublishAsync()` works the same way.

## Docker Compose changes

### Remove RabbitMQ from default docker-compose.yml

Delete the `rabbitmq` service definition and `rabbitmq_data` volume from `docker/docker-compose.yml`.

### Create docker-compose.rabbitmq.yml

Move the RabbitMQ service to an opt-in override file at `docker/docker-compose.rabbitmq.yml`:

```yaml
services:
  rabbitmq:
    image: rabbitmq:4.2-management-alpine
    container_name: ${COMPOSE_PROJECT_NAME:-foundry}-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
      - ./rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  rabbitmq_data:
```

To start RabbitMQ when needed:

```bash
cd docker && docker compose -f docker-compose.yml -f docker-compose.rabbitmq.yml up -d
```

### Clean up appsettings.Development.json

Remove the `RabbitMq` connection string and `RabbitMQ` host/port/credentials block, since they are no longer needed by default.

## Test environment

Tests use in-memory transport by default. The `test-inbox` queue declaration and `ListenToRabbitQueue("test-inbox")` call live inside the RabbitMQ transport block, so they naturally disappear when the transport is `InMemory`. No test rewrites are needed — handlers receive the same events through local queues.

## Restoring RabbitMQ

To switch back:

1. Set `ModuleMessaging:Transport` to `RabbitMq` in the target environment's appsettings
2. Provide the `RabbitMq` connection string
3. Start the RabbitMQ container (via the override compose file or managed service)

No code changes required.

## Scope

| File | Change |
|------|--------|
| `src/Foundry.Api/Program.cs` | Replace connection-string check with transport flag |
| `src/Foundry.Api/appsettings.json` | Add `ModuleMessaging.Transport` defaulting to `InMemory` |
| `src/Foundry.Api/appsettings.Development.json` | Remove RabbitMQ connection string and config block |
| `docker/docker-compose.yml` | Remove `rabbitmq` service and `rabbitmq_data` volume |
| `docker/docker-compose.rabbitmq.yml` | New file — RabbitMQ as opt-in override |

Total: ~15 lines changed, 1 new file, 0 handler changes.
