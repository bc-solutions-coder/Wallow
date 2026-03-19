# Auth Spec Revision Verification

**Date:** 2026-03-18
**Spec reviewed:** `docs/superpowers/specs/2026-03-18-authentication-architecture-design.md`

---

## Review Item Verification

### 1. [AllowAnonymous] on ShowcasesController — PASS

The spec documents this in Section 9 (Security Guarantees) under "No Anonymous Access" (line 1065):

> **Known exception**: `ShowcasesController.GetAll` and `ShowcasesController.GetById` currently have `[AllowAnonymous]`. This contradicts the auth architecture and must be removed. Showcases should require authentication — public showcase access goes through the BFF's service account, not anonymous access. Tracked as `foundry-oopm`.

Bead reference `foundry-oopm` is present. Clear explanation of the desired end state (BFF service account, not anonymous).

### 2. API key scope subset validation — PASS

The spec documents the validation algorithm in Section 5 under "Scope Enforcement" (lines 614-642):

1. Get user's current roles from JWT `realm_access.roles` claim
2. Expand roles to permissions via `RolePermissionMapping.GetPermissions()`
3. For each requested scope, call `MapScopeToPermission()` to get the `PermissionType`
4. If any `PermissionType` is NOT in the user's expanded permissions, reject with 403

Includes a worked example showing both success and rejection cases. Implementation status clearly marked as "NOT yet implemented" and "P0 production blocker" with bead reference `foundry-hsp5` (line 642).

### 3. X-Tenant-Id restricted to operator service accounts (sa-*) only — PASS

The spec explicitly documents this in Section 2 (Middleware Pipeline), in the TenantResolutionMiddleware description (lines 111-115):

> X-Tenant-Id header override: allowed only for realm admins (admin role in realm_access) and OPERATOR service accounts (azp starts with "sa-"). Developer apps (azp starts with "app-") are NOT allowed to override tenant context — they operate within the tenant of the user who authorized them, or are tenant-agnostic (TenantId.Platform).

Also reinforced in the Mental Model table (line 48-49) and the prefix convention table (lines 357-361) where `app-*` shows "**Not allowed**" for X-Tenant-Id Override.

### 4. API key PostgreSQL persistence — PASS

Section 5 under "Key Format and Storage" (lines 572-583) describes the dual-write pattern in detail:

- PostgreSQL (Identity schema): `ApiKeys` table as source of truth
- Valkey (cache): hash lookup and management lookup entries
- Read path: Valkey first, PostgreSQL fallback, repopulate cache
- Write path: PostgreSQL first (durable), then Valkey (cache)

Implementation note on line 583 correctly states:

> The current implementation uses Valkey-only storage (`RedisApiKeyService`). PostgreSQL dual-write is a required enhancement tracked as `foundry-u49d`.

### 5. DCR rate limiting — PASS

Section 7 (Rate Limiting) includes "Layer 4: DCR Registration Rate Limiter" (lines 886-896):

- Describes the Foundry proxy endpoint `POST /api/v1/identity/apps/register`
- Rate limit: 5 registrations per hour per authenticated user ID
- Applies only to developer self-service (Mode B, `app-*` prefix)
- Implementation status: "not yet implemented. Tracked as `foundry-i5y6`"

The proxy endpoint is also described in detail in Section 3 Mode B (lines 266-288) with the validation steps (prefix check, rate limit, scope whitelist).

### 6. app-* prefix for developer clients — PASS

All developer-registered client examples correctly use the `app-` prefix throughout:

- Mental Model table (line 38): `app-cool-viewer`
- Prefix tables (lines 46-50, 355-361): clear `sa-` vs `app-` distinction
- Mode B registration flow (lines 271, 284): `app-cool-showcase-viewer`
- Runtime flow example (lines 333, 347): `app-cool-showcase-viewer`
- Ecosystem topology diagram (lines 783-786): `app-cool-viewer`, `app-showcase-bot`, `app-inquiry-cli`
- Audit trail table (line 1079): `app-cool-viewer`

No instances of `sa-cool-viewer`, `sa-showcase-bot`, or `sa-inquiry-cli` were found. The `sa-*` prefix is used exclusively for operator clients (e.g., `sa-personal-site`, `sa-mobile-app`, `sa-admin-tool`, `sa-foundry-api`).

### 7. Error response specification — PASS

Section 9.5 (lines 1130-1173) provides a comprehensive error response specification covering:

- **401 Unauthorized**: 7 scenarios across all auth paths (no auth, invalid JWT, expired JWT, wrong audience, invalid API key format, key not found, key expired, invalid SCIM token)
- **403 Forbidden**: 4 scenarios (missing permission, lazy scope check, creation scope check, max keys reached)
- **429 Too Many Requests**: 4 scenarios with `Retry-After` header (global, per-key, module-specific, DCR registration)
- **400 Bad Request**: 4 scenarios (missing name, invalid scopes, missing tenant, missing app- prefix)

All responses follow RFC 7807 Problem Details format.

### 8. Revocation race condition — PASS

Section 5 under "Key Management" (lines 676-678) documents the race condition:

> **Revocation atomicity**: Revocation must delete the key from PostgreSQL first (durable), then Valkey (cache). The `UpdateLastUsedAsync` fire-and-forget task must check for key existence before writing to avoid a race condition where a concurrent `LastUsedAt` update re-creates a just-revoked key in Valkey. Implementation: use Valkey `SET ... NX` (set-if-not-exists) instead of `When.Always` in `UpdateLastUsedAsync`, or check a revoked flag.

Mitigation strategy is described (SET NX or revoked flag). Tracked as `foundry-5osr`.

### 9. Max 10 API keys per user — PASS

Section 5 under "Key Limits" (lines 644-666) documents:

- Maximum of 10 API keys per tenant per user
- Configurable via `appsettings.json` under `ApiKeys:MaxKeysPerUser`
- Error response body for exceeding the limit
- Implementation status: "not yet implemented. Tracked as `foundry-s5hr`"

### 10. Keycloak handles user registration — PASS

Section 6 under "User Registration" (lines 814-828) clearly states:

> User registration is handled entirely by Keycloak. Foundry does not implement custom registration logic — no password handling, no email verification, no account creation endpoints.

Lists the Keycloak-provided capabilities (self-registration, email verification, password policies, social login, MFA, account linking, brute-force detection). Also includes a future work note about lightweight user creation for inquiry submission.

### 11. Mobile OIDC — PASS

Two "future work" notes exist:

- Line 400 (end of Section 3): brief note that mobile OIDC is standard and will be documented in a future guide.
- Line 830 (Section 6): more detailed note: "A dedicated mobile integration guide will be created after the core auth architecture is implemented."

### 12. Multi-org user behavior — FAIL

No mention of multi-organization user behavior was found anywhere in the spec. There is no discussion of what happens when a user belongs to multiple Keycloak organizations, no "undefined" notation, and no reference to bead `foundry-9po7`. The `TenantResolutionMiddleware` description (Section 2) explains how a single `organization` claim is parsed but does not address the case where a user has multiple organization memberships.

### 13. No duplicate content with DCR spec — PASS

Section 3 opens with an explicit cross-reference on line 208:

> **For DCR implementation details** (realm export changes, configure-dcr.sh, docker compose integration), see the DCR implementation spec: `docs/superpowers/specs/2026-03-16-dynamic-client-registration-design.md`. This section covers the architecture and flows; the DCR spec covers the implementation steps.

The DCR spec is also listed in the Related Documents section (line 1279). Section 3 covers architecture and flows without duplicating the implementation details from the DCR spec.

---

## Additional Checks

### Stale sa-* references for developer clients

No instances of `sa-cool-viewer`, `sa-showcase-bot`, or `sa-inquiry-cli` found. All developer client examples correctly use the `app-` prefix.

### Table of Contents correctness

The TOC lists 11 entries (sections 1-10 plus 9.5). All anchor links match the generated heading slugs. Minor cosmetic note: TOC entries use colons ("Auth Path 1: DCR Service Accounts") while actual headings use em-dashes ("Auth Path 1 — DCR Service Accounts"), but this does not affect anchor link resolution since both produce the same slug. The "References" and "Related Documents" sections are omitted from the TOC, which is acceptable for appendix-style content.

### Bead ID references

All bead IDs are present and correctly referenced:

| Bead | Item | Location |
|------|------|----------|
| `foundry-oopm` | AllowAnonymous exception | Line 1065 |
| `foundry-hsp5` | Scope subset validation | Line 642 |
| `foundry-u49d` | PostgreSQL dual-write | Line 583 |
| `foundry-i5y6` | DCR rate limiting | Line 896 |
| `foundry-5osr` | Revocation race condition | Line 678 |
| `foundry-s5hr` | Max API keys limit | Line 666 |
| `foundry-9po7` | Multi-org user behavior | **MISSING** |

---

## Summary

| # | Item | Verdict |
|---|------|---------|
| 1 | [AllowAnonymous] on ShowcasesController | **PASS** |
| 2 | API key scope subset validation | **PASS** |
| 3 | X-Tenant-Id restricted to sa-* only | **PASS** |
| 4 | API key PostgreSQL persistence | **PASS** |
| 5 | DCR rate limiting | **PASS** |
| 6 | app-* prefix for developer clients | **PASS** |
| 7 | Error response specification | **PASS** |
| 8 | Revocation race condition | **PASS** |
| 9 | Max 10 API keys per user | **PASS** |
| 10 | Keycloak handles user registration | **PASS** |
| 11 | Mobile OIDC | **PASS** |
| 12 | Multi-org user behavior | **FAIL** |
| 13 | No duplicate content with DCR spec | **PASS** |

**Result: 12/13 PASS, 1 FAIL**

The single failing item is #12 (multi-org user behavior). The spec needs a note -- likely in Section 2 (TenantResolutionMiddleware) or Section 9 (Security Guarantees) -- documenting that the behavior when a user belongs to multiple Keycloak organizations is currently undefined, with a reference to `foundry-9po7`.
