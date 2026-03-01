# Audit Epic Definitions

Complete epic and task definitions for recreating in beads after backend migration.

---

## How to Use

After reinstalling beads with Dolt backend:

```bash
# 1. Initialize beads
bd init

# 2. Create epics and tasks from this document
# (Use bd create commands below, or import if bd supports it)

# 3. Set up dependencies
# (Use bd dep add commands below)
```

---

## Phase 0: Foundation

### Epic: foundation-tooling

**Purpose:** Audit-enabling tooling that helps subsequent audits.

```bash
bd create --type=epic --title="Foundation Tooling" --description="Audit-enabling infrastructure: analyzers, CI gates, quality tools. Complete before running audits." --priority=0
```

**Tasks:**

| ID | Title | Description | Priority | Status |
|----|-------|-------------|----------|--------|
| 1 | Configure .editorconfig | Set up comprehensive .editorconfig with all analyzer rules | P1 | DONE |
| 2 | Configure Directory.Build.props | Enable analyzers, nullable, implicit usings globally | P1 | DONE |
| 3 | Enable TreatWarningsAsErrors in CI | CI fails on any warning | P1 | DONE |
| 4 | Configure Qodana quality gates | Set up Qodana with thresholds and GitHub workflow | P1 | DONE |
| 5 | Establish StyleCop baseline | Configure StyleCop rules, document exceptions | P1 | DONE |
| 6 | Verify zero warnings | Confirm all projects compile with zero warnings | P1 | IN_PROGRESS |

```bash
# Create tasks (adjust epic ID after creation)
bd create --parent=<epic-id> --type=task --title="Configure .editorconfig" --description="Set up comprehensive .editorconfig with all analyzer rules" --priority=1
bd create --parent=<epic-id> --type=task --title="Configure Directory.Build.props" --description="Enable analyzers, nullable, implicit usings globally" --priority=1
bd create --parent=<epic-id> --type=task --title="Enable TreatWarningsAsErrors in CI" --description="CI fails on any warning" --priority=1
bd create --parent=<epic-id> --type=task --title="Configure Qodana quality gates" --description="Set up Qodana with thresholds and GitHub workflow" --priority=1
bd create --parent=<epic-id> --type=task --title="Establish StyleCop baseline" --description="Configure StyleCop rules, document any exceptions in docs/STYLECOP_BASELINE.md" --priority=1
bd create --parent=<epic-id> --type=task --title="Verify zero warnings" --description="Confirm all projects compile with zero warnings. Run: dotnet build --verbosity minimal" --priority=1
```

---

### Epic: foundation-audit

**Purpose:** Audit all foundation components. Output: audit reports in `docs/audits/`.

```bash
bd create --type=epic --title="Foundation Audit" --description="Systematic audit of all foundation components. Each task produces an audit report in docs/audits/. Uses checklists from docs/plans/2026-02-16-audit-checklists.md" --priority=0
```

**Tasks:**

#### Build & Tooling Audits
| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit build configuration | Verify build settings, dependencies, package management | Build & Tooling |

#### DDD Primitives Audits
| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit Entity<T> base class | Verify DDD compliance, equality, ID handling | DDD Primitives |
| Audit AggregateRoot<T> | Verify domain event handling, consistency boundaries | DDD Primitives |
| Audit value object base | Verify equality, immutability patterns | DDD Primitives |
| Audit strongly-typed IDs | Verify type safety, EF Core conversion | DDD Primitives |
| Audit Result<T> pattern | Verify error handling, success/failure flow | DDD Primitives |
| Audit Result extensions | Verify Map, Bind, Match helpers | DDD Primitives |
| Audit auditable patterns | Verify CreatedAt, ModifiedBy tracking | DDD Primitives |

#### Shared Infrastructure Audits
| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit ITenantContext | Verify tenant resolution, scoping | Shared Infrastructure |
| Audit tenant query filters | Verify EF Core global filters | Shared Infrastructure |
| Audit tenant Dapper helpers | Verify SQL query tenant injection | Shared Infrastructure |
| Audit EF Core base configuration | Verify conventions, interceptors | Shared Infrastructure |
| Audit EF Core auditing interceptor | Verify Created/Modified tracking | Shared Infrastructure |
| Audit Marten configuration | Verify event store setup | Shared Infrastructure |
| Audit Marten projection base | Verify projection conventions | Shared Infrastructure |
| Audit Wolverine integration | Verify handler discovery, outbox | Shared Infrastructure |

#### Architecture Tests Audits
| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit CleanArchitectureTests | Verify coverage, fix failures | Architecture Tests |
| Audit ModuleIsolationTests | Verify cross-boundary detection | Architecture Tests |
| Audit CqrsConventionTests | Verify Command/Query separation | Architecture Tests |
| Audit MultiTenancyArchitectureTests | Verify ITenantScoped enforcement | Architecture Tests |

#### API Infrastructure Audits
| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit JWT validation middleware | Verify token validation, claims | API Infrastructure |
| Audit permission middleware | Verify HasPermission attribute | API Infrastructure |
| Audit global exception handler | Verify error mapping, logging | API Infrastructure |
| Audit Result to HTTP mapping | Verify status codes, response format | API Infrastructure |
| Audit OpenAPI configuration | Verify completeness, accuracy | API Infrastructure |
| Audit API versioning strategy | Verify headers/URL routing | API Infrastructure |

#### Test Infrastructure Audits
| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit test fixtures | Verify shared setup, cleanup | Test Infrastructure |
| Audit test builders | Verify entity/DTO builders | Test Infrastructure |
| Audit fake implementations | Verify in-memory repos, mocks | Test Infrastructure |
| Audit Testcontainers setup | Verify Postgres, RabbitMQ, Keycloak | Test Infrastructure |
| Audit integration test base | Verify common patterns | Test Infrastructure |

```bash
# Example create commands (adjust epic ID)
bd create --parent=<epic-id> --type=task --title="Audit Entity<T> base class" --description="Verify DDD compliance, equality implementation, ID handling. Use DDD Primitives checklist. Output: docs/audits/YYYY-MM-DD-entity-base-audit.md" --priority=1
bd create --parent=<epic-id> --type=task --title="Audit AggregateRoot<T>" --description="Verify domain event handling, consistency boundaries. Use DDD Primitives checklist. Output: docs/audits/YYYY-MM-DD-aggregate-root-audit.md" --priority=1
# ... continue for all tasks
```

---

### Epic: foundation-impl

**Purpose:** Implement fixes from foundation audits. Created AFTER all audits complete.

```bash
bd create --type=epic --title="Foundation Implementation" --description="Implement fixes discovered during foundation audits. Each task references a design doc and includes per-task verification." --priority=0
```

**Tasks:** Created dynamically from grouped audit findings. See workflow design doc.

Example task structure:
```bash
bd create --parent=<epic-id> --type=task --title="[Theme] implementation" --description="Implement fixes for [theme]. Design doc: docs/plans/YYYY-MM-DD-[theme]-implementation.md. Verification: tests pass, code review." --priority=1
```

---

### Epic: foundation-verify

**Purpose:** Final verification after all foundation implementations complete.

```bash
bd create --type=epic --title="Foundation Verification" --description="Final verification of foundation phase. Run after all implementation tasks complete." --priority=0
```

**Tasks:**

| Title | Description |
|-------|-------------|
| Run full test suite | `dotnet test` - all tests pass |
| Run architecture tests | All architecture tests pass |
| Verify zero warnings | `dotnet build` with zero warnings |
| Performance baseline check | No regressions from baseline |
| Security scan | No new vulnerabilities |
| Final code review | Code review agent verifies all quality gates |

```bash
bd create --parent=<epic-id> --type=task --title="Run full test suite" --description="Execute dotnet test across entire solution. All tests must pass." --priority=1
bd create --parent=<epic-id> --type=task --title="Run architecture tests" --description="Execute all architecture tests. Verify Clean Architecture, module isolation, CQRS conventions." --priority=1
bd create --parent=<epic-id> --type=task --title="Verify zero warnings" --description="Run dotnet build --verbosity minimal. Must show 0 warnings." --priority=1
bd create --parent=<epic-id> --type=task --title="Performance baseline check" --description="Run performance tests. Verify no regressions from established baselines." --priority=1
bd create --parent=<epic-id> --type=task --title="Security scan" --description="Run security scanning tools. Verify no new vulnerabilities introduced." --priority=1
bd create --parent=<epic-id> --type=task --title="Final code review" --description="Code review agent runs full quality gates checklist against foundation code." --priority=1
```

---

## Phases 1-25: Module Audits

Each module follows the same pattern. Modules are ordered by dependency.

### Module Order

| Phase | Module | Type | Dependencies |
|-------|--------|------|--------------|
| 1 | Identity | Platform Infrastructure | Foundation |
| 2 | Billing | Platform Infrastructure | Identity |
| 3 | Email | Platform Infrastructure | Identity |
| 4 | Notifications | Platform Infrastructure | Identity, Email |
| 5 | Storage | Platform Infrastructure | Identity |
| 6 | Assets | Domain Building Block | Identity, Storage |
| 7 | Catalog | Domain Building Block | Identity, Assets |
| 8 | Inventory | Event-Sourced | Identity, Catalog |
| 9 | Sales | Event-Sourced | Identity, Catalog, Inventory |
| 10 | Scheduling | Event-Sourced | Identity, Catalog |
| 11 | AuditLog | Operational | Identity |
| 12 | Activity | Operational | Identity |
| 13 | Metering | Operational | Identity, Billing |
| 14 | Compliance | Operational | Identity, AuditLog |
| 15 | FeatureFlags | Operational | Identity |
| 16 | Configuration | System | Identity |
| 17 | Scheduler | System | Identity |
| 18 | Workflows | System | Identity, Scheduler |
| 19 | Comments | System | Identity |
| 20 | Reporting | System | Identity, multiple modules |
| 21 | Support | User-Facing | Identity, Email |
| 22 | KnowledgeBase | User-Facing | Identity, Assets |
| 23 | Announcements | User-Facing | Identity |
| 24 | StatusPage | User-Facing | Identity |
| 25 | Onboarding | User-Facing | Identity, multiple modules |

### Module Epic Template

For each module, create three epics:

```bash
# Audit epic (create upfront)
bd create --type=epic --title="[Module] Audit" --description="Systematic audit of [Module] module. Each task produces an audit report. Uses checklists from docs/plans/2026-02-16-audit-checklists.md" --priority=1

# Implementation epic (create after audits complete)
bd create --type=epic --title="[Module] Implementation" --description="Implement fixes discovered during [Module] audits. Each task references a design doc." --priority=1

# Verification epic (create after implementations complete)
bd create --type=epic --title="[Module] Verification" --description="Final verification of [Module] phase." --priority=1
```

### Module Audit Tasks Template

Each module audit epic has these standard tasks:

| Title | Description | Checklist |
|-------|-------------|-----------|
| Audit [Module] Domain layer | Entities, value objects, domain events, services | Module Domain |
| Audit [Module] Application layer | Commands, queries, handlers, validators, DTOs | Module Application |
| Audit [Module] Infrastructure layer | Repositories, DbContext, consumers, clients | Module Infrastructure |
| Audit [Module] API layer | Controllers, contracts, documentation | Module API |
| Audit [Module] existing tests | Test coverage, test quality, gaps | Test Infrastructure |

```bash
# Example for Identity module
bd create --parent=<epic-id> --type=task --title="Audit Identity Domain layer" --description="Audit entities, value objects, domain events, services. Use Module Domain checklist. Output: docs/audits/YYYY-MM-DD-identity-domain-audit.md" --priority=1
bd create --parent=<epic-id> --type=task --title="Audit Identity Application layer" --description="Audit commands, queries, handlers, validators, DTOs. Use Module Application checklist. Output: docs/audits/YYYY-MM-DD-identity-application-audit.md" --priority=1
bd create --parent=<epic-id> --type=task --title="Audit Identity Infrastructure layer" --description="Audit repositories, DbContext, consumers, clients. Use Module Infrastructure checklist. Output: docs/audits/YYYY-MM-DD-identity-infrastructure-audit.md" --priority=1
bd create --parent=<epic-id> --type=task --title="Audit Identity API layer" --description="Audit controllers, contracts, documentation. Use Module API checklist. Output: docs/audits/YYYY-MM-DD-identity-api-audit.md" --priority=1
bd create --parent=<epic-id> --type=task --title="Audit Identity existing tests" --description="Audit test coverage, test quality, identify gaps. Use Test Infrastructure checklist. Output: docs/audits/YYYY-MM-DD-identity-tests-audit.md" --priority=1
```

### Module Verification Tasks Template

Each module verification epic has these standard tasks:

| Title | Description |
|-------|-------------|
| Run [Module] test suite | All module tests pass |
| Verify [Module] architecture | Architecture tests pass for module |
| Verify [Module] zero warnings | No warnings in module code |
| [Module] code review | Code review agent verifies quality gates |

---

## Dependencies

### Phase Dependencies

```bash
# Foundation phases
bd dep add foundation-audit foundation-tooling      # Audits depend on tooling
bd dep add foundation-impl foundation-audit         # Impl depends on all audits
bd dep add foundation-verify foundation-impl        # Verify depends on all impls

# Module phases depend on foundation
bd dep add identity-audit foundation-verify         # Identity audit after foundation complete

# Module chain (each depends on previous)
bd dep add billing-audit identity-verify
bd dep add email-audit identity-verify
bd dep add notifications-audit email-verify
# ... continue for all modules per dependency table
```

### Within Epic Dependencies

Tasks within an epic generally have no dependencies (can be parallelized), except:
- Module audit tasks should be done in order: Domain → Application → Infrastructure → API → Tests
- Verification tasks depend on all implementation tasks

---

## Quick Setup Script

After beads reinstall, run these commands to set up Phase 0:

```bash
#!/bin/bash

# Create Foundation Tooling epic
TOOLING_EPIC=$(bd create --type=epic --title="Foundation Tooling" --description="Audit-enabling infrastructure" --priority=0 --json | jq -r '.id')

# Create tooling tasks (most already done)
bd create --parent=$TOOLING_EPIC --type=task --title="Configure .editorconfig" --priority=1
bd create --parent=$TOOLING_EPIC --type=task --title="Configure Directory.Build.props" --priority=1
bd create --parent=$TOOLING_EPIC --type=task --title="Enable TreatWarningsAsErrors in CI" --priority=1
bd create --parent=$TOOLING_EPIC --type=task --title="Configure Qodana quality gates" --priority=1
bd create --parent=$TOOLING_EPIC --type=task --title="Establish StyleCop baseline" --priority=1
bd create --parent=$TOOLING_EPIC --type=task --title="Verify zero warnings" --priority=1

# Create Foundation Audit epic
AUDIT_EPIC=$(bd create --type=epic --title="Foundation Audit" --description="Systematic audit of all foundation components" --priority=0 --json | jq -r '.id')

# Create audit tasks...
# (Continue with all audit task creations)

# Set up dependencies
bd dep add $AUDIT_EPIC $TOOLING_EPIC
```

---

## Notes

- Task titles should be concise (< 60 chars)
- Descriptions should reference checklist and output file
- Priority: P0 for epics, P1 for tasks
- Close tasks with `--reason` documenting key findings or completion evidence
