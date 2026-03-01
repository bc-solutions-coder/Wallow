# Foundry Codebase Perfection Audit Design

## Overview

**Objective:** Bring all 25 modules to enterprise-grade quality through systematic auditing and remediation, with verifiable proof of completion.

**Success Criteria:**
- All architecture tests pass (Clean Architecture, DDD, module isolation, CQRS)
- 100% unit test coverage for domain and application logic
- Integration tests for all infrastructure and API endpoints
- Zero linting warnings, zero analyzer issues
- All async code correct (proper await, cancellation token propagation)
- Comments are concise, grammatically correct, and only where needed
- Performance verified (no N+1, query tests for read paths)
- Security verified (tenant isolation, no injection vulnerabilities)

**Approach:** Hybrid - Foundation First, Then Modules
1. Perfect the shared infrastructure that all modules depend on
2. Audit modules in dependency order
3. For each module: audit → discover issues → create fix tasks → verify

**Verification Model:** Dual-gate
- Gate 1: All tests pass (unit, integration, architecture)
- Gate 2: Code-review agent verifies against checklist

**Tracking:** Beads issue tracker
- One epic per phase (Foundation + each module)
- Discovery-based tasks spawned during audits
- Session-per-module to manage context

---

## Quality Gates Checklist

Every module must pass ALL gates before closing its epic.

### Architecture & Design Gates

| Gate | Verification Method |
|------|---------------------|
| Clean Architecture layers (no inward violations) | Architecture tests |
| DDD tactical patterns correct | Code review |
| No cross-module direct references | Architecture tests |
| CQRS separation (Commands vs Queries) | Architecture tests |
| Sealed/virtual surface area (DTOs immutable, value objects sealed) | Architecture tests |

### Test Coverage Gates

| Gate | Verification Method |
|------|---------------------|
| Unit tests: Domain logic (entities, value objects, services) | Coverage report |
| Unit tests: Application handlers (commands, queries) | Coverage report |
| Integration tests: Infrastructure (repositories, services) | Test run |
| Integration tests: API endpoints | Test run |
| Architecture tests for module | Test run |
| Concurrency & race condition tests | Test run |
| Event sourcing projection correctness (for Marten modules) | Test run |

### Code Quality Gates

| Gate | Verification Method |
|------|---------------------|
| No dead code / unused files | Code review |
| Consistent naming conventions | Linting + review |
| Result pattern for errors (no exception-driven flow) | Code review |
| Validation on all commands | Code review |
| Async correctness (await, no async void, CancellationToken) | Analyzers + review |
| Zero linting/analyzer warnings (CI enforced) | Build output |
| Comments: correct grammar, concise, no redundancy | Code review |
| Code documentation standards (XML docs on public APIs) | Code review |

### Data & Messaging Gates

| Gate | Verification Method |
|------|---------------------|
| Data consistency & transaction boundaries verified | Integration tests |
| Message idempotency & delivery guarantees | Integration tests |
| Configuration validation at startup | Startup tests |

### Performance & Security Gates

| Gate | Verification Method |
|------|---------------------|
| Query performance tests with regression baseline | Performance tests |
| No N+1 query patterns | Code review + tests |
| Multi-tenancy enforcement depth (Dapper queries filter by tenant) | Integration tests |
| Security tests (negative cases - 403s, cross-tenant blocked) | Security tests |
| Observer/subscription memory leak checks | Memory tests |

### API & Infrastructure Gates

| Gate | Verification Method |
|------|---------------------|
| API versioning & backward compatibility | API tests |
| API contract consistency (responses match OpenAPI) | Contract tests |
| Health check coverage (all dependencies) | Health tests |

---

## Phase Structure

### Phase 0: Foundation

Single epic with ~55 granular tasks. Must complete before any module work begins.

#### Build & Tooling Tasks

| Task | Scope |
|------|-------|
| Audit current build warnings | Catalog all existing warnings by category |
| Configure analyzers | Enable/configure all .NET analyzers |
| Enable warnaserror in CI | CI fails on any warning |
| Configure Qodana gates | Enable quality gates, set thresholds |
| Establish StyleCop baseline | Configure rules, document exceptions |
| Verify all projects compile cleanly | Zero warnings across solution |

#### Shared Kernel Tasks

| Task | Scope |
|------|-------|
| Audit Entity<T> base class | Verify DDD compliance, equality, ID handling |
| Audit AggregateRoot<T> | Domain event handling, consistency boundaries |
| Audit auditable patterns | CreatedAt, ModifiedBy, etc. |
| Audit Result<T> pattern | Error handling, success/failure flow |
| Audit Result extensions | Map, Bind, Match helpers |
| Audit strongly-typed IDs | Type safety, EF Core conversion |
| Audit value object base | Equality, immutability |
| Add unit tests for Entity<T> | Full coverage |
| Add unit tests for AggregateRoot<T> | Full coverage |
| Add unit tests for Result<T> | Full coverage |
| Add unit tests for value objects | Full coverage |

#### Shared Infrastructure Tasks

| Task | Scope |
|------|-------|
| Audit ITenantContext | Tenant resolution, scoping |
| Audit tenant query filters | EF Core global filters |
| Audit tenant Dapper helpers | SQL query tenant injection |
| Audit EF Core base configuration | Conventions, interceptors |
| Audit EF Core auditing interceptor | Created/Modified tracking |
| Audit Marten configuration | Event store setup |
| Audit Marten projection base | Projection conventions |
| Audit Wolverine integration | Handler discovery, outbox |
| Add integration tests for multi-tenancy | Tenant isolation verified |
| Add integration tests for EF conventions | Auditing, soft delete |

#### Architecture Tests Tasks

| Task | Scope |
|------|-------|
| Audit CleanArchitectureTests | Verify coverage, fix failures |
| Audit ModuleIsolationTests | Cross-boundary detection |
| Audit CqrsConventionTests | Command/Query separation |
| Audit MultiTenancyArchitectureTests | ITenantScoped enforcement |
| Add concurrency architecture tests | Async patterns, CancellationToken |
| Add sealed/immutability tests | DTOs, value objects |
| Add message idempotency tests | Handler idempotency patterns |
| Add API contract tests | Response immutability, OpenAPI match |
| Add tenant query depth tests | Dapper SQL analysis |

#### API Infrastructure Tasks

| Task | Scope |
|------|-------|
| Audit JWT validation middleware | Token validation, claims |
| Audit permission middleware | HasPermission attribute |
| Audit global exception handler | Error mapping, logging |
| Audit Result to HTTP mapping | Status codes, response format |
| Audit OpenAPI configuration | Completeness, accuracy |
| Audit API versioning strategy | Headers/URL routing |
| Add negative auth tests | 401/403 scenarios |
| Add API versioning tests | Version routing works |
| Add error response tests | Consistent error format |

#### Test Infrastructure Tasks

| Task | Scope |
|------|-------|
| Audit test fixtures | Shared setup, cleanup |
| Audit test builders | Entity/DTO builders |
| Audit fake implementations | In-memory repos, mocks |
| Audit Testcontainers setup | Postgres, RabbitMQ, Keycloak |
| Audit integration test base | Common patterns |
| Establish performance baselines | Query timing thresholds |
| Add configuration validation tests | Startup validation |
| Add health check tests | All dependencies covered |
| Add memory leak detection setup | Observer cleanup verification |

### Phases 1-25: Module Audits

One epic per module, processed in dependency order.

| Phase | Module | Type | Depends On |
|-------|--------|------|------------|
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

---

## Module Audit Workflow

Each module follows this discovery-based workflow:

### Bead Structure Per Module

**Audit Beads (created upfront):**
- Audit [Module] Domain layer
- Audit [Module] Application layer
- Audit [Module] Infrastructure layer
- Audit [Module] API layer
- Audit [Module] existing tests

**Fix Beads (spawned during audits):**
- Created as issues are discovered during audit beads
- Example: "[FIX] User entity missing equality override"
- Example: "[FIX] CreateUserCommand missing validation"

**Verify Beads (created upfront):**
- Verify all [Module] tests pass
- Verify architecture tests pass for [Module]
- Verify zero linting warnings in [Module]
- Code review verification for [Module]

### Session Workflow

| Session | Work | Outcome |
|---------|------|---------|
| A | Audit Domain | Close audit bead, spawn fix beads |
| B | Audit Application | Close audit bead, spawn fix beads |
| C | Audit Infrastructure | Close audit bead, spawn fix beads |
| D | Audit API | Close audit bead, spawn fix beads |
| E | Audit Tests | Close audit bead, spawn fix beads |
| F+ | Fix issues | Work through fix beads |
| Final | Verify | Close verify beads, close epic |

---

## Verification Process

Each phase ends with verification beads that must pass before the epic closes.

### Verification Bead Requirements

| Verify Bead | What It Checks | Pass Criteria |
|-------------|----------------|---------------|
| All tests pass | `dotnet test` for module | Exit code 0, no skipped tests |
| Architecture tests pass | Module-specific arch tests | No violations |
| Zero linting warnings | `dotnet build` for module | No warnings in module files |
| Code review verification | Code-review agent runs checklist | All gates checked |

### Code Review Checklist

The code-review agent verifies all quality gates are met:

**Architecture & Design:**
- Clean Architecture layers verified
- DDD patterns correctly applied
- No cross-module references
- CQRS separation correct
- Sealed/immutable where required

**Code Quality:**
- No dead code
- Naming conventions consistent
- Result pattern used for errors
- All commands have validation
- Async code correct
- Comments concise and grammatically correct

**Data & Messaging:**
- Transaction boundaries correct
- Message idempotency handled
- Configuration validated

**Performance & Security:**
- No N+1 patterns
- Tenant isolation enforced in queries
- Security negative tests exist

**API:**
- Endpoints documented
- Contracts match OpenAPI
- Health checks present

---

## Implementation Notes

### Context Management

- Each phase is designed to fit within a single session's context
- Phase 0 tasks are granular enough to complete individually
- Module audits create beads during discovery to preserve findings
- Fix work can span multiple sessions using `bd ready`

### Parallel Execution

- Foundation tasks within a category can be parallelized
- Module phases must be sequential (dependency order)
- Fix beads within a phase can be parallelized

### Recovery

- All state is in beads (survives context loss)
- Run `bd ready` to resume work
- Run `bd show <epic>` to see phase progress
