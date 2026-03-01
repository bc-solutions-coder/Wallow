# Code Style and Conventions

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Commands | Verb + Noun + Command | `CompleteTaskCommand` |
| Queries | Get + Noun + Query | `GetTaskByIdQuery` |
| Handlers | Command/Query name + Handler | `CompleteTaskHandler` |
| Events | Noun + Past tense + Event | `TaskCompletedEvent` |
| DTOs | Noun + Dto | `TaskDto` |
| Requests | Verb + Noun + Request | `CreateTaskRequest` |
| Responses | Noun + Response | `TaskResponse` |

## File Organization

- **One class per file** - file name matches class name
- Commands and queries get their own folder:
  ```
  Commands/
  └── CompleteTask/
      ├── CompleteTaskCommand.cs
      └── CompleteTaskHandler.cs
  ```

## Architecture Rules

### Module Dependency Rules
```
Domain         → (no dependencies)
Application    → Domain only
Infrastructure → Application, Domain
Api            → Application only
```

### Cross-Module Communication
- Modules only reference `Shared.Contracts` for cross-module communication
- Events are **facts about what happened**, not commands
- Each module owns its own database tables (separate schemas)
- Use EF Core for writes, Dapper for complex read queries

**Never reference:**
- One module from another module directly (use events)
- Infrastructure from Api (use Application interfaces)

## Pattern Usage

### Command/Query Separation
- Commands for write operations (return void or entity)
- Queries for read operations (can use Dapper directly)

### Validation
- Use FluentValidation for all commands/queries
- Validators registered automatically via MediatR pipeline

### Domain Events
1. Define in `Shared.Contracts`
2. Publish from handlers via `IPublishEndpoint`
3. Consume in other modules via MassTransit consumers

## Database Conventions

- Each module has its own DbContext
- Use separate PostgreSQL schemas per module (e.g., `task_management`)
- Migrations scoped to module's Infrastructure project
