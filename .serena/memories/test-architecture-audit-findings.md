# Test Architecture Audit Findings (2026-02-21)

## Audit Scope
7 parallel agents analyzed 49 test projects across 22 modules. Reports in `tests/audit-*.md`.

## Critical Issues Found

### Naming Chaos (5 conventions across 49 projects)
- 28/49 projects missing `Foundry.` prefix
- 7 modules have mixed conventions internally
- Duplicate tests in Activity module
- Standard defined: `Foundry.{Module}.Tests` + `Foundry.{Module}.IntegrationTests`

### Shared Infrastructure Problems
- 6/8 builders are `internal` (dead code externally)
- 16+ inline PostgreSQL container creations (should use shared fixture)
- 3 WebApplicationFactory subclasses with copy-pasted code
- 3 modules duplicate SetTestUser/SetAdminUser
- No shared DB-only integration test base
- No shared API integration test base

### Event-Sourcing Gaps
- Scheduling doesn't use shared MartenFixture (uses postgres:16 - BUG)
- No concurrency tests in Sales or Scheduling (critical gap)
- No compliance/erasure tests in Sales or Scheduling
- Only Inventory has low-level Marten event stream tests

### Integration Test Issues
- Some modules use IClassFixture (3 containers per class) instead of ICollectionFixture
- Environment.SetEnvironmentVariable race condition risk
- Mixed cleanup strategies (RemoveRange vs TRUNCATE vs fresh tenant)

## What's Working Well
- Unit test patterns are mostly consistent (Method_Condition_Result naming)
- Central architecture tests cover all 24 modules comprehensively
- Testcontainers used consistently for infrastructure
- Good builder pattern in Tests.Common (just needs public visibility)
- Billing domain tests are gold standard for unit tests
- Inventory is gold standard for event-sourced tests

## Implementation Plan
Epic: `foundry-aph` with 15 implementation beads (7 audits closed).
Dependency chain: infra visibility -> base classes -> collection fixtures -> rename projects.
P1 (do first): shared infra visibility, DB base class, API base class, Scheduling fixes, concurrency tests.
P2 (then): GlobalUsings, rename projects, MessagingTestFixture refactor, collection fixtures, compliance tests, Marten tests.
P3 (last): remove redundant arch tests, add Wolverine convention tests, add WithCleanUp.

## CLAUDE.md Created
Comprehensive `tests/CLAUDE.md` with standards for naming, unit tests, integration tests, event sourcing, architecture tests.
