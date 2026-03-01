# Audit Checklists (v1)

Context-adaptive checklists for each audit category. These are living documents - add new checks as they're discovered during audits.

---

## How to Use

1. Select the checklist matching your audit category
2. Run through each check, recording evidence or findings
3. If you discover a check that should exist but doesn't, add it
4. Mark the checklist version used in your audit report

---

## 1. Build & Tooling Checklist

**Applies to:** CI configuration, analyzers, linting, build scripts

### Analyzer Configuration
- [ ] .editorconfig exists and is comprehensive
  - Evidence/Finding:
- [ ] Directory.Build.props configures analyzers globally
  - Evidence/Finding:
- [ ] Nullable reference types enabled (`<Nullable>enable</Nullable>`)
  - Evidence/Finding:
- [ ] Implicit usings configured appropriately
  - Evidence/Finding:

### CI/CD
- [ ] TreatWarningsAsErrors enabled in CI
  - Evidence/Finding:
- [ ] All projects compile with zero warnings
  - Evidence/Finding:
- [ ] Build artifacts are properly configured
  - Evidence/Finding:

### Code Quality Gates
- [ ] Qodana configured with quality gates
  - Evidence/Finding:
- [ ] StyleCop rules configured (or baseline documented)
  - Evidence/Finding:
- [ ] Code coverage thresholds defined
  - Evidence/Finding:

### Dependencies
- [ ] Central package management enabled (Directory.Packages.props)
  - Evidence/Finding:
- [ ] No deprecated packages
  - Evidence/Finding:
- [ ] No known vulnerable packages
  - Evidence/Finding:

---

## 2. DDD Primitives Checklist

**Applies to:** Entity<T>, AggregateRoot<T>, Value Objects, Domain Events

### Entity<T>
- [ ] Uses strongly-typed ID with IStronglyTypedId constraint
  - Evidence/Finding:
- [ ] Identity-based equality (Equals compares by ID)
  - Evidence/Finding:
- [ ] GetHashCode uses ID
  - Evidence/Finding:
- [ ] Equality operators (==, !=) implemented
  - Evidence/Finding:
- [ ] ID is immutable (protected set or init)
  - Evidence/Finding:
- [ ] No business logic leakage into base class
  - Evidence/Finding:

### AggregateRoot<T>
- [ ] Inherits from Entity<T>
  - Evidence/Finding:
- [ ] Domain events collection exists
  - Evidence/Finding:
- [ ] AddDomainEvent method available
  - Evidence/Finding:
- [ ] ClearDomainEvents method available
  - Evidence/Finding:
- [ ] Events are raised, not published directly
  - Evidence/Finding:

### Value Objects
- [ ] Immutable (no public setters)
  - Evidence/Finding:
- [ ] Structural equality (Equals compares all properties)
  - Evidence/Finding:
- [ ] GetHashCode uses all properties
  - Evidence/Finding:
- [ ] Sealed class (no inheritance)
  - Evidence/Finding:
- [ ] Validation in constructor
  - Evidence/Finding:

### Domain Events
- [ ] Immutable records or sealed classes
  - Evidence/Finding:
- [ ] Named in past tense (OrderCreated, not CreateOrder)
  - Evidence/Finding:
- [ ] Contain only data, no behavior
  - Evidence/Finding:
- [ ] Include correlation/causation IDs where needed
  - Evidence/Finding:

---

## 3. Shared Infrastructure Checklist

**Applies to:** TenantContext, EF config, Marten, Wolverine integration

### Multi-Tenancy
- [ ] ITenantContext interface defined
  - Evidence/Finding:
- [ ] TenantId available in scoped context
  - Evidence/Finding:
- [ ] Tenant resolution happens early in pipeline
  - Evidence/Finding:
- [ ] Missing tenant throws clear exception
  - Evidence/Finding:

### EF Core Configuration
- [ ] Global query filters for soft delete
  - Evidence/Finding:
- [ ] Global query filters for tenant isolation
  - Evidence/Finding:
- [ ] Auditing interceptor (CreatedAt, ModifiedAt, CreatedBy, ModifiedBy)
  - Evidence/Finding:
- [ ] Strongly-typed ID value converters
  - Evidence/Finding:
- [ ] Snake_case naming convention (if PostgreSQL)
  - Evidence/Finding:
- [ ] Optimistic concurrency (RowVersion/xmin)
  - Evidence/Finding:

### Marten Configuration
- [ ] Event store connection configured
  - Evidence/Finding:
- [ ] Projections registered
  - Evidence/Finding:
- [ ] Async daemon configured (if using async projections)
  - Evidence/Finding:
- [ ] Multi-tenancy mode set correctly
  - Evidence/Finding:

### Wolverine Integration
- [ ] Handlers discovered automatically
  - Evidence/Finding:
- [ ] Outbox configured for reliability
  - Evidence/Finding:
- [ ] RabbitMQ routing configured
  - Evidence/Finding:
- [ ] Retry policies defined
  - Evidence/Finding:

### Dapper Helpers
- [ ] Tenant ID injected into queries
  - Evidence/Finding:
- [ ] SQL injection prevention (parameterized queries)
  - Evidence/Finding:
- [ ] Connection management correct (using statements)
  - Evidence/Finding:

---

## 4. Architecture Tests Checklist

**Applies to:** CleanArchitectureTests, ModuleIsolationTests, CqrsConventionTests

### Clean Architecture Tests
- [ ] Domain layer has no infrastructure dependencies
  - Evidence/Finding:
- [ ] Application layer only references Domain
  - Evidence/Finding:
- [ ] Infrastructure can reference Application and Domain
  - Evidence/Finding:
- [ ] API can reference Application (not Domain directly for handlers)
  - Evidence/Finding:
- [ ] No circular references
  - Evidence/Finding:

### Module Isolation Tests
- [ ] Modules don't reference each other directly
  - Evidence/Finding:
- [ ] Cross-module communication via Shared.Contracts only
  - Evidence/Finding:
- [ ] No shared DbContext between modules
  - Evidence/Finding:

### CQRS Convention Tests
- [ ] Commands end with "Command"
  - Evidence/Finding:
- [ ] Queries end with "Query"
  - Evidence/Finding:
- [ ] Command handlers don't return domain entities
  - Evidence/Finding:
- [ ] Queries are read-only (no side effects)
  - Evidence/Finding:

### Naming Convention Tests
- [ ] Handlers end with "Handler"
  - Evidence/Finding:
- [ ] Validators end with "Validator"
  - Evidence/Finding:
- [ ] Repositories end with "Repository"
  - Evidence/Finding:

### Immutability Tests
- [ ] DTOs are immutable (records or init-only)
  - Evidence/Finding:
- [ ] Commands are immutable
  - Evidence/Finding:
- [ ] Queries are immutable
  - Evidence/Finding:
- [ ] Events are immutable
  - Evidence/Finding:

---

## 5. API Infrastructure Checklist

**Applies to:** JWT validation, permissions, exception handling, OpenAPI

### Authentication
- [ ] JWT validation configured correctly
  - Evidence/Finding:
- [ ] Token expiration handled
  - Evidence/Finding:
- [ ] Refresh token flow (if applicable)
  - Evidence/Finding:
- [ ] Claims extracted and available
  - Evidence/Finding:

### Authorization
- [ ] Permission-based authorization (not just role-based)
  - Evidence/Finding:
- [ ] HasPermission attribute or equivalent
  - Evidence/Finding:
- [ ] Tenant isolation enforced at API level
  - Evidence/Finding:
- [ ] 403 returned for unauthorized (not 401)
  - Evidence/Finding:

### Exception Handling
- [ ] Global exception handler exists
  - Evidence/Finding:
- [ ] Exceptions mapped to appropriate HTTP status codes
  - Evidence/Finding:
- [ ] Stack traces not exposed in production
  - Evidence/Finding:
- [ ] Correlation ID included in error responses
  - Evidence/Finding:

### Result to HTTP Mapping
- [ ] Result<T> maps to appropriate status codes
  - Evidence/Finding:
- [ ] Validation failures return 400 with details
  - Evidence/Finding:
- [ ] Not found returns 404
  - Evidence/Finding:
- [ ] Conflict returns 409
  - Evidence/Finding:

### OpenAPI
- [ ] All endpoints documented
  - Evidence/Finding:
- [ ] Request/response schemas accurate
  - Evidence/Finding:
- [ ] Authentication requirements documented
  - Evidence/Finding:
- [ ] Examples provided where helpful
  - Evidence/Finding:

### API Versioning
- [ ] Versioning strategy defined (URL, header, query)
  - Evidence/Finding:
- [ ] Default version specified
  - Evidence/Finding:
- [ ] Deprecation policy documented
  - Evidence/Finding:

---

## 6. Test Infrastructure Checklist

**Applies to:** Fixtures, builders, fakes, Testcontainers

### Test Fixtures
- [ ] Shared fixtures for expensive resources (DB, containers)
  - Evidence/Finding:
- [ ] Proper cleanup between tests
  - Evidence/Finding:
- [ ] Async disposal implemented
  - Evidence/Finding:
- [ ] Collection fixtures for parallel test isolation
  - Evidence/Finding:

### Test Builders
- [ ] Builder pattern for complex entities
  - Evidence/Finding:
- [ ] Sensible defaults provided
  - Evidence/Finding:
- [ ] Fluent API for customization
  - Evidence/Finding:
- [ ] Bogus/Faker integration for random data
  - Evidence/Finding:

### Fake Implementations
- [ ] In-memory repositories for unit tests
  - Evidence/Finding:
- [ ] Fake message bus for handler isolation
  - Evidence/Finding:
- [ ] Fake tenant context for multi-tenancy tests
  - Evidence/Finding:
- [ ] Fakes match interface contracts
  - Evidence/Finding:

### Testcontainers Setup
- [ ] PostgreSQL container configured
  - Evidence/Finding:
- [ ] RabbitMQ container configured (if needed)
  - Evidence/Finding:
- [ ] Keycloak container configured (if needed)
  - Evidence/Finding:
- [ ] Container reuse enabled for speed
  - Evidence/Finding:
- [ ] Health checks before tests run
  - Evidence/Finding:

### Integration Test Base
- [ ] WebApplicationFactory configured
  - Evidence/Finding:
- [ ] Test authentication bypass available
  - Evidence/Finding:
- [ ] Database seeding helpers
  - Evidence/Finding:
- [ ] Cleanup between tests
  - Evidence/Finding:

---

## 7. Module Domain Checklist

**Applies to:** Per-module domain layer audit

### Entities
- [ ] All entities inherit from Entity<T> or AggregateRoot<T>
  - Evidence/Finding:
- [ ] Strongly-typed IDs used
  - Evidence/Finding:
- [ ] Encapsulation enforced (no public setters for invariants)
  - Evidence/Finding:
- [ ] Factory methods for complex creation
  - Evidence/Finding:
- [ ] Domain methods express ubiquitous language
  - Evidence/Finding:

### Value Objects
- [ ] Used for concepts with no identity
  - Evidence/Finding:
- [ ] Immutable
  - Evidence/Finding:
- [ ] Self-validating
  - Evidence/Finding:

### Domain Events
- [ ] Events raised for significant state changes
  - Evidence/Finding:
- [ ] Event names match domain language
  - Evidence/Finding:
- [ ] Events contain necessary data (no lazy loading)
  - Evidence/Finding:

### Domain Services
- [ ] Used only for cross-aggregate operations
  - Evidence/Finding:
- [ ] Stateless
  - Evidence/Finding:
- [ ] Interface defined in Domain layer
  - Evidence/Finding:

### Repository Interfaces
- [ ] Defined in Domain layer
  - Evidence/Finding:
- [ ] Return domain types (not DTOs)
  - Evidence/Finding:
- [ ] Aggregate-focused (one repo per aggregate root)
  - Evidence/Finding:

### No Infrastructure Leakage
- [ ] No EF Core references in Domain
  - Evidence/Finding:
- [ ] No Marten references in Domain
  - Evidence/Finding:
- [ ] No external service references
  - Evidence/Finding:

---

## 8. Module Application Checklist

**Applies to:** Per-module application layer audit

### Commands
- [ ] Named with verb (CreateOrder, UpdateUser)
  - Evidence/Finding:
- [ ] Immutable (record or sealed with init)
  - Evidence/Finding:
- [ ] Contains only data needed for operation
  - Evidence/Finding:
- [ ] Validator exists for each command
  - Evidence/Finding:

### Command Handlers
- [ ] Single responsibility (one command, one handler)
  - Evidence/Finding:
- [ ] Returns Result<T> or Result
  - Evidence/Finding:
- [ ] CancellationToken propagated
  - Evidence/Finding:
- [ ] No direct infrastructure access (uses interfaces)
  - Evidence/Finding:

### Queries
- [ ] Named with noun (GetOrder, ListUsers)
  - Evidence/Finding:
- [ ] Immutable
  - Evidence/Finding:
- [ ] Read-only (no side effects)
  - Evidence/Finding:

### Query Handlers
- [ ] Returns DTOs (not domain entities)
  - Evidence/Finding:
- [ ] Optimized for read (can use Dapper)
  - Evidence/Finding:
- [ ] Pagination for list queries
  - Evidence/Finding:

### DTOs
- [ ] Immutable (record types)
  - Evidence/Finding:
- [ ] No domain logic
  - Evidence/Finding:
- [ ] Flat structure (avoid deep nesting)
  - Evidence/Finding:

### Validators
- [ ] FluentValidation used
  - Evidence/Finding:
- [ ] All business rules validated
  - Evidence/Finding:
- [ ] Clear error messages
  - Evidence/Finding:
- [ ] Async validation for DB checks (if needed)
  - Evidence/Finding:

### Interfaces
- [ ] Repository interfaces used (not concrete classes)
  - Evidence/Finding:
- [ ] External service interfaces defined
  - Evidence/Finding:
- [ ] No infrastructure types in signatures
  - Evidence/Finding:

---

## 9. Module Infrastructure Checklist

**Applies to:** Per-module infrastructure layer audit

### Repositories
- [ ] Implements interface from Application/Domain
  - Evidence/Finding:
- [ ] Uses EF Core for writes
  - Evidence/Finding:
- [ ] Uses Dapper for complex reads (if applicable)
  - Evidence/Finding:
- [ ] Tenant filtering applied
  - Evidence/Finding:
- [ ] Soft delete filtering applied
  - Evidence/Finding:

### DbContext
- [ ] Module has its own DbContext
  - Evidence/Finding:
- [ ] Schema isolation (separate schema per module)
  - Evidence/Finding:
- [ ] Entity configurations in separate files
  - Evidence/Finding:
- [ ] Migrations exist and are up to date
  - Evidence/Finding:

### Event Consumers
- [ ] Idempotency handled
  - Evidence/Finding:
- [ ] Error handling/retry configured
  - Evidence/Finding:
- [ ] Dead letter queue for failures
  - Evidence/Finding:
- [ ] Logging for debugging
  - Evidence/Finding:

### External Service Clients
- [ ] Implements interface from Application
  - Evidence/Finding:
- [ ] Resilience policies (retry, circuit breaker)
  - Evidence/Finding:
- [ ] Timeout configured
  - Evidence/Finding:
- [ ] Logging for debugging
  - Evidence/Finding:

### Module Registration
- [ ] AddXxxModule extension method exists
  - Evidence/Finding:
- [ ] InitializeXxxModuleAsync for migrations
  - Evidence/Finding:
- [ ] Registered in FoundryModules.cs
  - Evidence/Finding:

---

## 10. Module API Checklist

**Applies to:** Per-module API layer audit

### Controllers
- [ ] Thin controllers (delegate to handlers)
  - Evidence/Finding:
- [ ] Proper HTTP verbs used
  - Evidence/Finding:
- [ ] Route conventions followed
  - Evidence/Finding:
- [ ] Authorization attributes present
  - Evidence/Finding:

### Request/Response Contracts
- [ ] Request models are immutable
  - Evidence/Finding:
- [ ] Response models are immutable
  - Evidence/Finding:
- [ ] Validation attributes (if not using FluentValidation)
  - Evidence/Finding:
- [ ] Proper nullability annotations
  - Evidence/Finding:

### Error Handling
- [ ] Returns appropriate status codes
  - Evidence/Finding:
- [ ] Error responses follow standard format
  - Evidence/Finding:
- [ ] Validation errors include field names
  - Evidence/Finding:

### Documentation
- [ ] XML comments on public endpoints
  - Evidence/Finding:
- [ ] ProducesResponseType attributes
  - Evidence/Finding:
- [ ] Summary and description provided
  - Evidence/Finding:

### Security
- [ ] Tenant isolation enforced
  - Evidence/Finding:
- [ ] Sensitive data not logged
  - Evidence/Finding:
- [ ] Input validation present
  - Evidence/Finding:
- [ ] No injection vulnerabilities
  - Evidence/Finding:

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| v1 | 2026-02-16 | Initial checklists based on quality gates design |

---

## Contributing

When running an audit and discovering a check that should exist:

1. Add the check to the appropriate section
2. Mark it with `[NEW]` temporarily
3. After the audit, consolidate new checks into this document
4. Update the version history
