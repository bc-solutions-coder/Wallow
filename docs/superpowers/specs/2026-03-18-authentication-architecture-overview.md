# Foundry Authentication Architecture — Design Overview

**Date:** 2026-03-18
**Companion to:** `2026-03-18-authentication-architecture-design.md` (full spec)

This document captures the high-level design decisions and rationale discussed during the brainstorming session, before the formal spec was written. It's a readable summary for anyone who wants the "why" without the implementation detail.

---

## Section 1: Mental Model

Every request to Foundry must carry an identity. There are no anonymous endpoints. Three authentication paths exist, each for a different caller type:

| Path | Caller | Auth Method | Identity | Example |
|------|--------|-------------|----------|---------|
| **Service Account** | Apps (yours or third-party) | OAuth2 `client_credentials` via Keycloak | The app itself (`sa-personal-site`, `sa-cool-viewer`) | BFF submitting inquiry, third-party showcase aggregator |
| **User Session** | Logged-in humans via browser | OIDC authorization code + PKCE via Keycloak | The user (`sub` claim = Keycloak user ID) | User managing their profile in a web app |
| **API Key** | Developers, scripts, integrations | `X-Api-Key` header with `sk_live_*` key | The developer who created the key (user ID + tenant) | Developer testing endpoints, CI/CD automation |

**Key principle**: Service accounts represent *apps*. API keys represent *people accessing programmatically*. User sessions represent *people using apps interactively*. All three resolve to a `ClaimsPrincipal` with permissions — the rest of the pipeline treats them identically.

Service accounts have two registration modes, both using the same Keycloak DCR endpoint:

| Mode | Auth | Who | Use Case |
|------|------|-----|----------|
| **Operator-provisioned** | Initial Access Token | Platform operator (you) | Your BFF, mobile backend, internal tools |
| **Developer self-service** | Bearer Token (developer's JWT) | Any user with `create-client` role | Third-party apps, community integrations |

Both produce `sa-*` clients in Keycloak. Keycloak policies control what each mode is permitted to do.

```
                    ┌─────────────────────┐
                    │   Foundry API        │
                    │                      │
   sa-* token ──────►                     │
   (your app OR     │  All three paths     │
    3rd-party app)  │  → ClaimsPrincipal  │──► Same authorization pipeline
                    │  → TenantContext     │
   User JWT ────────►  → Permissions      │
                    │                      │
   X-Api-Key ───────►                     │
                    │                      │
                    └─────────────────────┘
```

---

## Section 2: Middleware Pipeline

The middleware executes in strict order. Each layer adds context for the next:

```
Request arrives
  │
  ▼
┌─────────────────────────────────┐
│ 1. Rate Limiter                 │  Global: 1000 req/hr per identity
│    (ASP.NET Core built-in)      │  Module-specific policies (auth, upload, SCIM)
├─────────────────────────────────┤
│ 2. ApiKeyAuthenticationMiddleware│  Checks X-Api-Key header
│                                 │  If present → validate via Valkey → set ClaimsPrincipal
│                                 │  If absent → fall through to JWT
├─────────────────────────────────┤
│ 3. Authentication               │  Keycloak JWT Bearer validation
│    (ASP.NET Core + Keycloak)    │  Validates signature, audience (aud: foundry-api), expiry
├─────────────────────────────────┤
│ 4. TenantResolutionMiddleware   │  Reads 'organization' claim → sets ITenantContext
│                                 │  Service accounts + admins can override via X-Tenant-Id
├─────────────────────────────────┤
│ 5. TenantBaggageMiddleware      │  Sets OpenTelemetry tags for observability
├─────────────────────────────────┤
│ 6. ScimAuthenticationMiddleware │  Only for /scim/v2/* endpoints
│                                 │  Bearer token → SHA256 hash → constant-time compare
├─────────────────────────────────┤
│ 7. PermissionExpansionMiddleware│  Users: role claims → PermissionType claims
│                                 │  Service accounts + API keys: scope → PermissionType
├─────────────────────────────────┤
│ 8. Authorization                │  Checks [HasPermission(X)] attributes
│    (ASP.NET Core)               │  403 if missing required permission
├─────────────────────────────────┤
│ 9. ServiceAccountTrackingMiddleware│  Fire-and-forget: updates LastUsedAt
│                                 │  Lazy sync: auto-creates metadata for unknown DCR clients
└─────────────────────────────────┘
  │
  ▼
Controller action executes
```

**Critical ordering rules:**
- API key middleware runs *before* JWT auth — if a key is present, JWT is bypassed entirely
- Tenant resolution must happen *after* authentication (needs claims to read)
- Permission expansion must happen *after* tenant resolution (needs tenant context)
- Authorization must happen *after* permission expansion (needs permission claims)
- Service account tracking runs *after* authorization (only tracks successful auth)

---

## Section 3: Auth Path 1 — DCR Service Accounts

This path is for **any app that needs to call the Foundry API** — whether you built it or a third-party developer did.

### Mode A: Operator-Provisioned (Your Apps)

For apps you control — BFFs, mobile backends, internal tools. You create an Initial Access Token in the Keycloak Admin Console and use it to register the client.

```
App startup (one-time):
┌──────────────┐  POST /realms/foundry/clients-registrations/openid-connect  ┌──────────┐
│  Your App    │──────────────────────────────────────────────────────────────►│ Keycloak │
│  (BFF)       │  Auth: Bearer <initial-access-token>                         │          │
│              │  Body: { client_id: "sa-personal-site", ... }                │          │
│              │◄──────────────────────────────────────────────────────────────│          │
│              │  { client_id, client_secret, registration_access_token }     └──────────┘
└──────────────┘
```

- Initial Access Tokens are single-use, time-limited, created by admin
- Admin assigns scopes directly via Keycloak Admin Console or Foundry API
- Pre-configured clients (like `sa-foundry-api`) can be defined in the realm export

### Mode B: Developer Self-Service (Third-Party Apps)

For apps built by anyone in the community. The developer registers as a user, authenticates, and uses their own JWT to register a client via the same DCR endpoint.

```
Developer registers their app:
┌──────────────┐  POST /realms/foundry/clients-registrations/openid-connect  ┌──────────┐
│  Developer   │──────────────────────────────────────────────────────────────►│ Keycloak │
│              │  Auth: Bearer <developer's own JWT>                           │          │
│              │  Body: { client_id: "sa-cool-viewer", ... }                  │          │
│              │◄──────────────────────────────────────────────────────────────│          │
│              │  { client_id, client_secret, registration_access_token }     └──────────┘
└──────────────┘
```

**Requirements:**
- Developer must have the `create-client` Keycloak role (auto-assigned or on request)
- Keycloak policies enforce constraints:

| Policy | Purpose | Configuration |
|--------|---------|---------------|
| **Client Scope Policy** | Whitelists which scopes DCR clients can request | Only allow `showcases.read`, `inquiries.write`, etc. Block admin scopes. |
| **Max Clients Policy** | Limits apps per developer | e.g., 10 apps per user |
| **Full Scope Policy** | Disabled by default | DCR clients only get explicitly assigned scopes |
| **Client Disabled Policy** | Optional admin approval | New clients created disabled until admin activates (prod option) |
| **Consent Required Policy** | User authorization screen | If app later uses authorization_code flow, users see consent |

### Common to Both Modes

**Runtime flow (identical):**

```
Every request:
┌──────────────┐  client_credentials grant  ┌──────────┐
│  App         │───────────────────────────►│ Keycloak │
│              │◄───────────────────────────│          │
│              │  { access_token: "jwt" }   └──────────┘
│              │
│              │  Authorization: Bearer <jwt>  ┌──────────┐
│              │──────────────────────────────►│ Foundry  │
│              │◄──────────────────────────────│ API      │
└──────────────┘                               └──────────┘
```

**Key characteristics:**
- Client IDs must use `sa-` prefix. Missing prefix → zero permissions → 403. Fail-safe.
- `ServiceAccountTrackingMiddleware` lazily creates `ServiceAccountMetadata` on first API call. Uses `TenantId.Platform` sentinel.
- `registration_access_token` lets the registrant manage their own client. Token rotates on each use.
- Developers manage their own clients; admins can view, modify, or revoke any client.

---

## Section 4: Auth Path 2 — User Sessions (OIDC)

This path is for **humans using apps interactively** — browsing your site, managing their account, logging into a dashboard.

**Flow:**

```
┌─────────┐     ┌──────────────┐     ┌──────────┐     ┌─────────┐
│ Browser │────►│  Your App    │────►│ Keycloak │────►│ Foundry │
│         │     │  (BFF/SPA)   │     │          │     │ API     │
└─────────┘     └──────────────┘     └──────────┘     └─────────┘

1. User clicks "Log in"
2. App redirects to Keycloak login page
3. User authenticates (email/password, social login, SSO)
4. Keycloak redirects back with authorization code
5. App exchanges code for tokens (authorization_code + PKCE)
6. App sends user's JWT to Foundry API
```

**JWT claims used by Foundry:**
- `sub` — Keycloak user ID
- `organization` — tenant ID (Keycloak 26+ JSON format or simple GUID)
- `realm_access.roles` — role array → expanded to permissions
- `azp` — authorized party (e.g., `foundry-spa`, not `sa-*`)
- `aud` — must include `foundry-api`

**Permission expansion:**

```
JWT roles: ["manager"]
  ↓ RolePermissionMapping.GetPermissions()
  ↓
13 permission claims added to ClaimsPrincipal
```

Three role tiers:
- **admin** — 47 permissions (everything)
- **manager** — 13 permissions (team management, API keys, showcases, config)
- **user** — 9 permissions (basic access: messaging, storage, showcases read, inquiries write)

**Key characteristics:**
- Foundry never handles passwords or generates tokens — Keycloak owns all of that
- Tenant context comes from the `organization` claim in the JWT
- PKCE is required — no implicit flow, no client_secret in the browser
- The `foundry-spa` client is pre-configured for this flow

---

## Section 5: Auth Path 3 — User API Keys

This path is for **developers who want personal programmatic access** — scripts, CLI tools, CI/CD automation, quick prototyping.

**Lifecycle:**

```
1. Developer registers as a user (Keycloak)
2. Developer logs in via OIDC session (Path 2)
3. Developer creates an API key via POST /api/v1/identity/auth/keys
   → Chooses a name ("my-showcase-aggregator")
   → Selects scopes (must be subset of their role's permissions)
   → Optionally sets expiration
   → Receives: sk_live_<base64url> (shown ONCE, never again)
4. Developer stores the key securely
5. Developer uses it in their app:
   GET /api/v1/showcases
   X-Api-Key: sk_live_a1b2c3d4...
```

**Key properties:**
- Format: `sk_live_<32 random bytes, base64url>` — recognized by GitHub secret scanning
- Stored as SHA256 hash in Valkey — the raw key is never persisted
- Scoped to a specific user + tenant + permission set
- **Scope enforcement**: requested scopes must be a subset of the user's current permissions
- Optional expiration — keys without expiry live until revoked
- `LastUsedAt` updated fire-and-forget on each use
- Revocation is immediate — delete from Valkey

**Liability model:**
- The key acts as the user. Leaked key → damage scoped to that user's permissions in one tenant.
- Developer responsible for secure storage.
- Admins can view and revoke any key.

**When to use API keys vs service accounts:**
- If the thing calling the API is *you* → API key
- If it's *an app you built* → service account

---

## Section 6: The Open Ecosystem

**Developer journey:**

```
Path A: "I want to quickly script against the API"
  1. Register as a user (Keycloak)
  2. Log in, create an API key
  3. Use X-Api-Key header — done

Path B: "I want to build an app"
  1. Register as a user (Keycloak)
  2. Get create-client role
  3. Authenticate, call DCR with your JWT
  4. Get client_id + client_secret
  5. Build your app using client_credentials flow

Path C: "I operate my own Foundry instance"
  1. Fork the repo, deploy your own stack
  2. Configure your own policies
  3. Your users follow Path A or B against YOUR instance
```

**Forked instances are completely independent.** Same code, separate Keycloak realms, separate user bases, separate access control. No hardcoded secrets, no phone-home, no license server.

---

## Section 7: Rate Limiting

Rate limiting operates in three layers:

**Layer 1: Global Rate Limiter (ASP.NET Core built-in)**

| Policy | Limit | Window |
|--------|-------|--------|
| Global | 1,000 requests | 1 hour |
| Auth | 3 requests | 10 minutes |
| Upload | 10 requests | 1 hour |
| SCIM | 30 requests | 1 minute |

Identity resolution: API key → `api_key_id`, service account → `azp`, user → `sub`.

**Layer 2: Per-Key Rate Limiter** — flat 1,000 req/hr per API key via Valkey.

**Layer 3: Module-Specific Limits** — e.g., inquiries: 5 submissions/15min per caller identity.

All layers stack. If any rejects, the request gets 429.

---

## Section 8: Permission Model

Two systems, unified by `PermissionExpansionMiddleware`:

**Role-based (users):** Keycloak roles → `RolePermissionMapping` → permission claims.
- admin: 47 permissions
- manager: 13 permissions
- user: 9 permissions

**Scope-based (service accounts + API keys):** OAuth2 scopes → `MapScopeToPermission()` → permission claims.
- 40+ scope-to-permission mappings across all modules
- Unknown scopes silently ignored

**API key scope constraint:** Requested scopes must be a subset of the creating user's current permissions. Prevents privilege escalation.

**Authorization enforcement:** `[HasPermission(PermissionType.X)]` → `PermissionAuthorizationHandler` checks for `permission` claim. Same check for all auth paths.

**Adding new permissions:** Update 5 files together: `PermissionType.cs`, `RolePermissionMapping.cs`, `PermissionExpansionMiddleware.cs`, `realm-export.json`, `ApiScopes.cs`.

---

## Section 9: Security Guarantees

**No anonymous access.** Every request authenticates. "Public" content is served via authenticated service accounts.

**Every request traces to an identity.** Service accounts → `azp`, users → `sub`, API keys → `api_key_id` + `sub`.

**Credential leak blast radius is bounded.** Every credential type has scoped permissions, time limits, and revocation mechanisms.

**Fail-safe defaults everywhere:**
- Missing `sa-` prefix → zero permissions → 403
- DCR clients → zero scopes until admin assigns them
- API keys with no scopes → zero permissions → 403
- Unknown JWT scopes → silently ignored → no permission granted
- `sk_live_` prefix → detected by GitHub secret scanning

---

## Section 10: For Platform Operators (Fork Guide)

**Keycloak configuration:** realm name, audience mapper, DCR policies, Initial Access Tokens, `create-client` role, Client Scope Policy, user registration, token lifetimes.

**Foundry configuration:** JWT audience, rate limits, role permissions, scope mappings, API key format, Valkey connection.

**Adding new module permissions:** 5 files updated together — `PermissionType.cs`, `RolePermissionMapping.cs`, `PermissionExpansionMiddleware.cs`, `realm-export.json`, `ApiScopes.cs`.

**Production checklist:** 20+ items covering Keycloak hardening, Foundry monitoring, and infrastructure security.

See the full spec for complete details on each section.
