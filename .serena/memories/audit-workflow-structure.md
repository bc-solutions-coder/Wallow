# Audit Workflow Structure

## Three-Phase Model

1. **Audit Phase** - Discover all issues, produce checklist-driven reports in `docs/audits/`
2. **Implementation Phase** - Fix issues based on grouped design docs, per-task verification
3. **Verification Phase** - Full regression, cross-cutting checks, final sign-off

## Key Principles

- Complete ALL audits before starting implementation
- Audit-enabling tooling (analyzers, CI gates) is implemented during audit phase
- Each implementation bead requires a design doc reference
- Findings are grouped by theme for batch implementation

## Epic Structure

### Foundation (Phase 0)
- `foundation-tooling` - Analyzers, StyleCop, Qodana, CI gates
- `foundation-audit` - All foundation audits
- `foundation-impl` - Created after audits complete
- `foundation-verify` - Final verification

### Modules (Phases 1-25)
- `[module]-audit` - Domain, Application, Infrastructure, API, Tests audits
- `[module]-impl` - Created after module audits complete
- `[module]-verify` - Final module verification

## Design Documents

- `docs/plans/2026-02-16-audit-workflow-design.md` - Full workflow design
- `docs/plans/2026-02-16-audit-checklists.md` - 10 context-adaptive checklists
- `docs/plans/2026-02-16-audit-epic-definitions.md` - Epic/task definitions for beads

## Audit Report Template

Location: `docs/audits/YYYY-MM-DD-[area]-audit.md`

Each report includes:
- Summary (pass/warning/fail counts)
- Checklist with evidence/findings
- Findings Summary table (ID, Finding, Location, Severity, Effort)

## Severity Scale
- Critical: Security hole, data loss, breaks functionality
- High: Architecture violation, edge case bugs
- Medium: Code smell, inconsistency
- Low: Style issue, nice-to-have

## Effort Scale
- Trivial: < 5 min
- Low: < 30 min
- Medium: 1-2 hours
- High: Half day+
