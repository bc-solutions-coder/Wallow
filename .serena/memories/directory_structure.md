# Directory Structure

## Current Structure (Initial State)
```
Foundry/
├── Foundry.sln          # Solution file
├── Foundry.csproj       # Initial project (to be refactored)
├── Program.cs
├── Worker.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── compose.yaml
├── CLAUDE.md                   # Claude Code instructions
├── docs/
│   ├── DEVELOPER_GUIDE.md
│   └── plans/
│       ├── 2026-02-04-foundry-pivot-design.md
│       ├── IMPLEMENTATION_PLAN.md
│       └── tasks/
│           └── phase-1-foundation/ through phase-9-examples/
└── .serena/                    # Serena configuration
```

## Target Structure (After Implementation)
```
Foundry/
├── Foundry.sln
├── src/
│   ├── Foundry.Api/             # Main host - wires modules
│   ├── Modules/
│   │   ├── Identity/
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/
│   │   │   └── Api/
│   │   ├── TaskManagement/
│   │   ├── Billing/
│   │   ├── Email/
│   │   └── Notifications/
│   └── Shared/
│       ├── Contracts/                  # Cross-module events/DTOs
│       └── Kernel/                     # Base classes, abstractions
├── tests/
│   └── Modules/
│       └── {Module}/
│           └── Modules.{Module}.Tests/
├── examples/                           # Isolated learning examples
├── docs/
└── docker/
    └── compose.yaml
```

## Module Internal Structure
```
Modules/{ModuleName}/
├── Foundry.{Module}.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Events/
├── Foundry.{Module}.Application/
│   ├── Commands/{CommandName}/
│   │   ├── {Command}Command.cs
│   │   └── {Command}Handler.cs
│   ├── Queries/{QueryName}/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Validations/
├── Foundry.{Module}.Infrastructure/
│   ├── Persistence/
│   │   ├── {Module}DbContext.cs
│   │   ├── Configurations/
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Consumers/
│   └── Services/
└── Foundry.{Module}.Api/
    └── Controllers/
```
