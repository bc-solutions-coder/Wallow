# Foundry Codebase Audit Workflow Design

## Overview

**Problem:** Mixing audit and implementation tasks in single epics causes context exhaustion. Audits and implementations have different goals:
- Audits = discovery, documentation, building the full picture
- Implementations = focused changes based on known requirements

**Solution:** Three-phase approach with clear separation.

## Three-Phase Model

```
┌─────────────────────────────────────────────────────────────┐
│ AUDIT PHASE                                                 │
│  Audit 1 → Audit 2 → ... → Audit N → All findings collected │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ IMPLEMENTATION PHASE                                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Impl Task 1 │  │ Impl Task 2 │  │ Impl Task N │          │
│  │ + Verify ✓  │  │ + Verify ✓  │  │ + Verify ✓  │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ VERIFICATION PHASE                                          │
│  Full test suite · Architecture tests · Cross-module checks │
│  Performance regression · Security scan · Final code review │
└─────────────────────────────────────────────────────────────┘
```

### Phase Definitions

| Phase | Goal | Output |
|-------|------|--------|
| **Audit Phase** | Discover all issues | Checklist-driven audit reports in `docs/audits/` |
| **Implementation Phase** | Fix issues systematically | Working code, per-task verification (tests pass, code review) |
| **Verification Phase** | Validate everything works together | Full regression, cross-cutting checks, final sign-off |

### Key Principles

1. Complete ALL audits before starting implementation
2. Audit-enabling tooling (analyzers, CI gates) is implemented during audit phase
3. Each implementation bead requires a design doc reference
4. Each implementation task verifies itself before closing
5. Final verification phase catches cross-cutting regressions
6. Findings are grouped by theme for efficient batch implementation

---

## Audit Report Template

Location: `docs/audits/YYYY-MM-DD-[area]-audit.md`

```markdown
# [Area] Audit - YYYY-MM-DD

## Summary
| Status | Count |
|--------|-------|
| ✅ Pass | X |
| ⚠️ Warning | X |
| ❌ Fail | X |

**Overall: PASS / PARTIAL / FAIL**

## Checklist

### [Category 1]
- [x] Check item description
  - Evidence: What proves this passes
- [ ] Check item that failed
  - Finding: What's wrong
  - Location: `path/to/file.cs:line`
  - Severity: Critical/High/Medium/Low
  - Effort: Trivial/Low/Medium/High

### [Category 2]
...

## Findings Summary

| ID | Finding | Location | Severity | Effort |
|----|---------|----------|----------|--------|
| F1 | Brief description | path:line | High | Low |
| F2 | Brief description | path:line | Medium | Medium |

## Notes
Any additional observations, context, or recommendations.
```

### Severity Scale

| Level | Meaning | Example |
|-------|---------|---------|
| **Critical** | Breaks functionality, security hole, data loss risk | Missing tenant filter on query |
| **High** | Violates architecture, causes bugs in edge cases | Cross-module direct reference |
| **Medium** | Code smell, maintenance burden, inconsistency | Missing validation on command |
| **Low** | Style issue, minor improvement, nice-to-have | Inconsistent naming |

### Effort Scale

| Level | Meaning | Example |
|-------|---------|---------|
| **Trivial** | < 5 minutes, single file | Add missing attribute |
| **Low** | < 30 minutes, few files | Add validation + tests |
| **Medium** | 1-2 hours, multiple files | Refactor method signatures |
| **High** | Half day+, architectural change | Change inheritance hierarchy |

---

## Implementation Design Doc Template

Location: `docs/plans/YYYY-MM-DD-[theme]-implementation.md`

Findings are grouped by theme (e.g., "Entity Equality", "Command Validation", "Tenant Filter Fixes") rather than one doc per finding.

```markdown
# [Theme] Implementation Design

## Overview
Brief description of what this implementation addresses.

## Related Findings

| Audit | Finding ID | Description | Severity | Effort |
|-------|------------|-------------|----------|--------|
| Entity<T> Audit | F1 | Missing equality override | High | Low |
| AggregateRoot Audit | F2 | Same issue in base class | High | Low |

## Scope

### In Scope
- Specific change 1
- Specific change 2

### Out of Scope
- What we're explicitly NOT doing

## Implementation Approach

### Changes Required
| File | Change |
|------|--------|
| `path/to/File.cs` | Add Equals() override |
| `path/to/OtherFile.cs` | Update base class |

### Code Examples
~~~csharp
// Before
public class Entity<T> { }

// After
public class Entity<T> : IEquatable<Entity<T>> { }
~~~

## Testing Strategy
- What tests to add/modify
- How to verify the fix works

## Risks & Mitigations
- Any breaking changes?
- Migration concerns?

## Verification Criteria
- [ ] All related tests pass
- [ ] No new warnings introduced
- [ ] Code review approved
- [ ] [Domain-specific criteria]
```

---

## Epic Structure

### Foundation (Phase 0)

| Epic | Purpose | When Created |
|------|---------|--------------|
| `foundation-tooling` | Audit-enabling tooling (analyzers, StyleCop, Qodana, CI gates) | Upfront |
| `foundation-audit` | All foundation audits | Upfront |
| `foundation-impl` | Foundation implementations | After all audits complete |
| `foundation-verify` | Final foundation verification | After all implementations complete |

### Modules (Phases 1-25)

Same pattern per module:

| Epic | Purpose | When Created |
|------|---------|--------------|
| `[module]-audit` | Module audits (Domain, Application, Infrastructure, API, Tests) | Upfront |
| `[module]-impl` | Module implementations | After module audits complete |
| `[module]-verify` | Module verification | After module implementations complete |

---

## Workflow

### Session Flow

| Session | Work | Command |
|---------|------|---------|
| 1 | Complete audit-enabling tooling | `bd ready` in foundation-tooling |
| 2-N | Run audits, create audit reports | `bd ready` in foundation-audit |
| N+1 | Review all findings, group by theme, create design docs | Manual review |
| N+2 | Create implementation beads from design docs | `bd create` in foundation-impl |
| N+3+ | Implement + verify each theme | `bd ready` in foundation-impl |
| Final | Run full verification | `bd ready` in foundation-verify |

### Foundation Phase Flow

```
foundation-tooling              foundation-audit
┌─────────────────┐            ┌─────────────────────┐
│ ✓ Analyzers     │───────────▶│ Audit Entity<T>     │
│ ✓ StyleCop      │   aids     │ Audit AggregateRoot │
│ ✓ Qodana        │            │ Audit EF Config     │
│ ✓ WarnAsError   │            │ ...                 │
└─────────────────┘            └─────────────────────┘
                                        │
                                        ▼
                               docs/audits/*.md
                               (all findings collected)
                                        │
                                        ▼
                               Group findings by theme
                                        │
                                        ▼
                               docs/plans/*-implementation.md
                                        │
                                        ▼
                               foundation-impl
                               ┌─────────────────────┐
                               │ Theme 1 + verify ✓  │
                               │ Theme 2 + verify ✓  │
                               │ ...                 │
                               └─────────────────────┘
                                        │
                                        ▼
                               foundation-verify
                               ┌─────────────────────┐
                               │ Full test suite     │
                               │ Architecture tests  │
                               │ Final code review   │
                               └─────────────────────┘
```

---

## Audit Categories

Context-adaptive checklists are defined per audit category. See `docs/plans/2026-02-16-audit-checklists.md` for the full checklist definitions.

| Category | Applies To | Focus |
|----------|------------|-------|
| Build & Tooling | CI, analyzers, linting | Toolchain configuration |
| DDD Primitives | Entity, AggregateRoot, Value Objects | Domain building blocks |
| Shared Infrastructure | TenantContext, EF config, Marten | Cross-cutting infrastructure |
| Architecture Tests | CleanArchitecture, ModuleIsolation, CQRS tests | Convention enforcement |
| API Infrastructure | JWT, permissions, exception handling | HTTP layer concerns |
| Test Infrastructure | Fixtures, builders, fakes, Testcontainers | Testing patterns |
| Module Domain | Per-module domain layer | Entities, events, services |
| Module Application | Per-module application layer | Commands, queries, handlers |
| Module Infrastructure | Per-module infrastructure | Repos, consumers, services |
| Module API | Per-module API layer | Controllers, contracts |

---

## Living Documents

Checklists are versioned and evolve:
- Initial checklists are "v1" - expected to evolve
- When running an audit, add new checks discovered during the process
- Periodically consolidate learnings back into the base templates
- Mark checklist version in each audit report

---

## Related Documents

- `docs/plans/2026-02-16-audit-checklists.md` - Context-adaptive checklists (v1)
- `docs/plans/2026-02-16-audit-epic-definitions.md` - Complete epic/task definitions for beads
- `docs/plans/2026-02-16-codebase-perfection-audit-design.md` - Original quality gates reference
