# Module Simplification — Implementation Plan

**Date:** 2026-02-27
**Design:** `2026-02-27-module-simplification-design.md`
**Branch:** Create from `main` before starting

## Phases

Each phase produces a buildable, testable codebase. No phase depends on a later phase. Complete each phase fully — including tests passing — before starting the next.

---

## Phase 1: Delete Unused Modules

**Goal:** Remove the 14 modules that are being cut. Get to a clean build with fewer projects.

**Why first:** Deletion is the simplest operation and immediately reduces noise. Everything downstream benefits from a smaller codebase.

### Tasks

1. **Remove module source projects** — Delete the following directories under `src/Modules/`:
   - Activity, Assets, Catalog, Comments, Compliance, Inventory, KnowledgeBase, Onboarding, Reporting, Sales, Scheduling, StatusPage, Support, Workflows

2. **Remove module test projects** — Delete corresponding directories under `tests/Modules/` for all 14 modules.

3. **Update FoundryModules.cs** — Remove all `Add*Module()` and `Initialize*ModuleAsync()` calls for deleted modules.

4. **Update solution file** — Remove deleted `.csproj` references from `Foundry.sln`.

5. **Clean Shared.Contracts** — Remove integration events that only deleted modules published or consumed. Keep events used by the 5 remaining modules.

6. **Remove Marten event sourcing config** — Remove projections and event stores for Sales, Inventory, and Scheduling from the shared Marten configuration.

7. **Remove orphaned API controllers** — Check `src/Foundry.Api/Controllers/` for any controllers that reference deleted modules.

8. **Remove unused package references** — Audit `Directory.Packages.props` and individual `.csproj` files for packages only used by deleted modules.

9. **Build and fix** — Run `dotnet build`. Fix any remaining compilation errors from dangling references.

10. **Run tests** — Run `dotnet test`. All remaining tests must pass.

---

## Phase 2: Merge Email + Notifications + Announcements into Communications

**Goal:** Three modules become one. Single `DbContext`, single schema, unified API.

### Tasks

1. **Create Communications project structure** — Create the four projects:
   - `Foundry.Communications.Domain`
   - `Foundry.Communications.Application`
   - `Foundry.Communications.Infrastructure`
   - `Foundry.Communications.Api`

2. **Migrate Email domain** — Move Email domain entities, value objects, and domain events into `Communications.Domain/Channels/Email/`. Rename namespaces.

3. **Migrate Notifications domain** — Move Notification entities and domain events into `Communications.Domain/Channels/InApp/`. Rename namespaces.

4. **Migrate Announcements domain** — Move Announcement entities into `Communications.Domain/Announcements/`. Rename namespaces.

5. **Create CommunicationsDbContext** — Single `DbContext` using the `communications` schema. Absorb entity configurations from all three modules.

6. **Migrate Application layer** — Move command/query handlers from all three modules into `Communications.Application/`. Update handler references to use the new `DbContext` and repositories.

7. **Migrate Infrastructure layer** — Move MailKit integration, SignalR hub, and persistence implementations into `Communications.Infrastructure/`.

8. **Migrate API layer** — Move controllers into `Communications.Api/`. Consolidate endpoints.

9. **Create EF migration** — Generate an initial migration for `CommunicationsDbContext`. Write a SQL migration script to rename existing tables from old schemas (`email`, `notifications`, `announcements`) to the `communications` schema.

10. **Update FoundryModules.cs** — Replace three separate module registrations with `AddCommunicationsModule()` and `InitializeCommunicationsModuleAsync()`.

11. **Update Shared.Contracts** — Rename integration events if namespaces changed. Ensure consuming modules still compile.

12. **Delete old module directories** — Remove `src/Modules/Email/`, `src/Modules/Notifications/`, `src/Modules/Announcements/`.

13. **Migrate tests** — Move and update tests from all three modules into `tests/Modules/Communications/`.

14. **Build, test, verify** — `dotnet build && dotnet test`. All tests pass.

---

## Phase 3: Merge Metering into Billing

**Goal:** Metering becomes a subdomain of Billing.

### Tasks

1. **Move Metering domain** — Move `UsageRecord`, `UsageMeter`, `RatingRule`, and related value objects into `Billing.Domain/Metering/`. Rename namespaces.

2. **Absorb Metering DbContext** — Add Metering entity configurations to `BillingDbContext`. Remove `MeteringDbContext`.

3. **Move Application handlers** — Move Metering command/query handlers into `Billing.Application/`. Update repository and `DbContext` references.

4. **Move Infrastructure** — Move Metering persistence, consumers, and background jobs into `Billing.Infrastructure/`.

5. **Move API endpoints** — Move usage-related controllers/endpoints into `Billing.Api/`. Add `UsageController` if one does not exist.

6. **Create EF migration** — Migrate Metering tables into the `billing` schema.

7. **Update FoundryModules.cs** — Remove `AddMeteringModule()`. Billing's registration now covers metering.

8. **Delete Metering module directory** — Remove `src/Modules/Metering/`.

9. **Migrate tests** — Move Metering tests into the Billing test projects.

10. **Build, test, verify** — `dotnet build && dotnet test`. All tests pass.

---

## Phase 4: Add Auditing (Audit.NET)

**Goal:** Automatic audit logging for all EF Core changes, replacing the AuditLog module.

### Tasks

1. **Add Audit.NET packages** — Add `Audit.NET` and `Audit.EntityFramework.Core` to `Directory.Packages.props`.

2. **Create AuditEntry entity** — Define `AuditEntry` in `Shared.Infrastructure/Auditing/`. Fields: Id, EntityType, EntityId, Action (Insert/Update/Delete), OldValues (JSON), NewValues (JSON), UserId, TenantId, Timestamp.

3. **Create audit DbContext or table** — Decide whether audit entries go through a dedicated `AuditDbContext` or are written directly via Audit.NET's data providers. Use the `audit` schema.

4. **Configure Audit.NET interceptor** — Register `AuditSaveChangesInterceptor` globally so every module's `DbContext` is covered. Configure in `Shared.Infrastructure/Auditing/AuditingExtensions.cs`.

5. **Wire into Program.cs** — Call `services.AddFoundryAuditing()` in the shared infrastructure setup.

6. **Remove AuditLog module** — Delete `src/Modules/AuditLog/` and its test projects (if not already deleted in Phase 1).

7. **Test** — Verify audit entries are created when entities are inserted, updated, and deleted. Write integration tests.

8. **Build, test, verify** — `dotnet build && dotnet test`. All tests pass.

---

## Phase 5: Add Background Job Abstraction

**Goal:** Replace the Scheduler module with a thin `IJobScheduler` abstraction over Hangfire.

### Tasks

1. **Define IJobScheduler interface** — Create in `Shared.Infrastructure/BackgroundJobs/`:
   ```csharp
   public interface IJobScheduler
   {
       string Enqueue(Expression<Func<Task>> methodCall);
       string Enqueue<T>(Expression<Func<T, Task>> methodCall);
       void AddRecurring(string id, string cron, Expression<Func<Task>> methodCall);
       void RemoveRecurring(string id);
   }
   ```

2. **Implement HangfireJobScheduler** — Thin wrapper that delegates to Hangfire's `BackgroundJob` and `RecurringJob` static classes.

3. **Register in DI** — Add `services.AddFoundryBackgroundJobs()` extension method. Wire into `Program.cs`.

4. **Migrate existing scheduled jobs** — Find any jobs in remaining modules that go through the Scheduler module. Update them to use `IJobScheduler` directly.

5. **Remove Scheduler module** — Delete `src/Modules/Scheduler/` and its test projects (if not already deleted in Phase 1).

6. **Test** — Verify modules can enqueue and schedule jobs through the abstraction.

7. **Build, test, verify** — `dotnet build && dotnet test`. All tests pass.

---

## Phase 6: Add Elsa 3 as Shared Infrastructure

**Goal:** Move Elsa from a standalone module to embedded shared infrastructure. Modules register their own activities and triggers.

### Tasks

1. **Create Elsa infrastructure** — Add `Shared.Infrastructure/Workflows/ElsaExtensions.cs` with `services.AddFoundryWorkflows()`. Configure Elsa 3 server runtime, PostgreSQL persistence, and API endpoints.

2. **Create WorkflowActivityBase** — Base class in `Shared.Infrastructure/Workflows/` that module-defined activities can extend.

3. **Wire into Program.cs** — Register Elsa runtime and mount API endpoints.

4. **Migrate existing activities** — Move any Workflow module activities that belong to remaining modules (e.g., Billing triggers) into those modules' Infrastructure layers.

5. **Remove Workflows module** — Delete `src/Modules/Workflows/` and its test projects (if not already deleted in Phase 1).

6. **Test** — Verify Elsa server starts, API endpoints respond, and module-registered activities are discoverable.

7. **Build, test, verify** — `dotnet build && dotnet test`. All tests pass.

---

## Phase 7: Final Cleanup and Verification

**Goal:** Ensure the codebase is clean, consistent, and fully tested.

### Tasks

1. **Update CLAUDE.md** — Revise the project overview, module list, architecture section, and module registration examples to reflect the new 5-module structure.

2. **Update README.md** — Revise project description and module list.

3. **Update Developer Guide** — Revise `docs/DEVELOPER_GUIDE.md` to reflect new structure, shared infrastructure additions, and module creation instructions.

4. **Update architecture tests** — Revise `NetArchTest` rules to validate the new module boundaries.

5. **Clean solution file** — Verify `Foundry.sln` contains only the projects that exist.

6. **Run full test suite** — `dotnet test` with all tests passing.

7. **Run build with warnings** — `dotnet build -warnaserror` to catch any new warnings.

8. **Verify Docker startup** — `cd docker && docker compose up -d`, then `dotnet run --project src/Foundry.Api`. Confirm the API starts and modules initialize.

---

## Phase Summary

| Phase | Description | Modules After |
|-------|-------------|---------------|
| 1 | Delete 14 unused modules | 10 (5 kept + 3 to merge + Scheduler + AuditLog) |
| 2 | Merge Email + Notifications + Announcements → Communications | 8 |
| 3 | Merge Metering → Billing | 7 |
| 4 | Add Audit.NET, remove AuditLog module | 6 |
| 5 | Add IJobScheduler, remove Scheduler module | 5 |
| 6 | Add Elsa 3 infrastructure, remove Workflows module | 5 (final) |
| 7 | Documentation and final cleanup | 5 (verified) |

## Notes

- Each phase produces a working, buildable, testable codebase.
- Phases 4, 5, and 6 are independent — they can run in parallel or in any order.
- Database migrations require care. Existing data in schemas owned by merged modules must be preserved or migrated.
- The Scheduler and Workflows modules may already be deleted in Phase 1. Phases 5 and 6 then focus only on adding the shared infrastructure replacement.
